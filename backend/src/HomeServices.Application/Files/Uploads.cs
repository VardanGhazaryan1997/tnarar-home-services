using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Errors;
using HomeServices.Application.Messaging;
using HomeServices.Domain;
using HomeServices.Domain.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Files;

/// <summary>Step 1 of an upload: get a signed link to PUT the file to.</summary>
public sealed record RequestUpload(string FileName, string ContentType, long Size) : ICommand<UploadTicket>;

/// <summary>Step 2, after the browser uploaded the file: check it and (for images) make a thumbnail.</summary>
public sealed record CompleteUpload(Guid FileId) : ICommand<FileDto>;

/// <summary>A file with fresh download links. Only its owner and staff can see it.</summary>
public sealed record GetFile(Guid FileId) : IQuery<FileDto>;

public sealed class RequestUploadValidator : AbstractValidator<RequestUpload>
{
    public RequestUploadValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().WithErrorCode("file_name.required");
        RuleFor(x => x.ContentType)
            .Must(StoredFile.IsAllowedType).WithErrorCode("content_type.not_allowed");
        RuleFor(x => x.Size)
            .GreaterThan(0).WithErrorCode("size.invalid")
            .Must((command, size) => size <= StoredFile.MaxSizeFor(StoredFile.KindOf(command.ContentType)))
            .WithErrorCode("size.too_large")
            .When(x => StoredFile.IsAllowedType(x.ContentType), ApplyConditionTo.CurrentValidator);
    }
}

public sealed class RequestUploadHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IFileStorage storage,
    IOptions<FileSettings> settings,
    TimeProvider clock) : ICommandHandler<RequestUpload, UploadTicket>
{
    public async Task<UploadTicket> HandleAsync(RequestUpload command, CancellationToken cancellationToken)
    {
        var (ownerType, ownerId) = FileOwner.Of(currentUser);
        var now = clock.GetUtcNow();

        var hourAgo = now.AddHours(-1);
        var recent = await db.Files.CountAsync(
            f => f.OwnerType == ownerType && f.OwnerId == ownerId && f.CreatedAt > hourAgo, cancellationToken);
        if (recent >= settings.Value.MaxUploadsPerHour)
        {
            throw new TooManyRequestsException("Too many uploads; try again later.", "file.too_many_uploads");
        }

        var file = StoredFile.Begin(ownerType, ownerId, command.FileName, command.ContentType, command.Size, now);
        db.Files.Add(file);
        await db.SaveChangesAsync(cancellationToken);

        var expiresAt = now.AddMinutes(settings.Value.UploadUrlLifetimeMinutes);
        var url = await storage.CreateUploadUrlAsync(file.Key, file.ContentType, expiresAt, cancellationToken);
        return new UploadTicket(
            file.Id,
            url.ToString(),
            "PUT",
            new Dictionary<string, string> { ["Content-Type"] = file.ContentType },
            expiresAt);
    }
}

public sealed class CompleteUploadHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IFileStorage storage,
    IImageProcessor images,
    FileDtoFactory dtos,
    TimeProvider clock) : ICommandHandler<CompleteUpload, FileDto>
{
    public async Task<FileDto> HandleAsync(CompleteUpload command, CancellationToken cancellationToken)
    {
        var (ownerType, ownerId) = FileOwner.Of(currentUser);
        var file = await db.Files.FirstOrDefaultAsync(f => f.Id == command.FileId, cancellationToken);
        if (file is null || !file.IsOwnedBy(ownerType, ownerId))
        {
            throw FileOwner.NotFound();
        }

        switch (file.Status)
        {
            case FileStatus.Ready:
                return await dtos.CreateAsync(file, cancellationToken);
            case FileStatus.Rejected:
                throw new DomainException(file.RejectionCode!, "This upload was rejected.");
        }

        var info = await storage.GetInfoAsync(file.Key, cancellationToken)
            ?? throw new DomainException("file.not_uploaded", "The file hasn't been uploaded yet.");

        if (info.Size > StoredFile.MaxSizeFor(file.Kind))
        {
            throw await RejectAsync(file, "file.too_large", cancellationToken);
        }

        var header = await storage.ReadStartAsync(file.Key, FileSignatures.HeaderLength, cancellationToken);
        if (!FileSignatures.Matches(file.ContentType, header))
        {
            throw await RejectAsync(file, "file.invalid_content", cancellationToken);
        }

        if (file.Kind == FileKind.Image)
        {
            ProcessedImage? image;
            await using (var original = await storage.OpenReadAsync(file.Key, cancellationToken))
            {
                image = images.Process(original, StoredFile.ThumbnailMaxSize);
            }

            if (image is null)
            {
                throw await RejectAsync(file, "file.invalid_content", cancellationToken);
            }

            var thumbnailKey = file.ThumbnailKeyFor();
            using (var thumbnail = new MemoryStream(image.ThumbnailJpeg))
            {
                await storage.PutAsync(thumbnailKey, thumbnail, "image/jpeg", cancellationToken);
            }

            file.MarkReady(info.Size, clock.GetUtcNow(), thumbnailKey, image.Width, image.Height);
        }
        else
        {
            file.MarkReady(info.Size, clock.GetUtcNow());
        }

        await db.SaveChangesAsync(cancellationToken);
        return await dtos.CreateAsync(file, cancellationToken);
    }

    // Deletes the stored object and records why; returns the error to throw.
    private async Task<DomainException> RejectAsync(StoredFile file, string code, CancellationToken cancellationToken)
    {
        await storage.DeleteAsync(file.Key, cancellationToken);
        file.Reject(code, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        return new DomainException(code, "The uploaded file was rejected.");
    }
}

public sealed class GetFileHandler(IAppDbContext db, ICurrentUser currentUser, FileDtoFactory dtos) : IQueryHandler<GetFile, FileDto>
{
    public async Task<FileDto> HandleAsync(GetFile query, CancellationToken cancellationToken)
    {
        var (ownerType, ownerId) = FileOwner.Of(currentUser);
        var file = await db.Files.AsNoTracking().FirstOrDefaultAsync(f => f.Id == query.FileId, cancellationToken);
        if (file is null || (ownerType != FileOwnerType.Staff && !file.IsOwnedBy(ownerType, ownerId)))
        {
            throw FileOwner.NotFound();
        }

        return await dtos.CreateAsync(file, cancellationToken);
    }
}

internal static class FileOwner
{
    public static (FileOwnerType Type, string Id) Of(ICurrentUser currentUser) =>
        currentUser.UserId is { } id
            ? (currentUser.IsStaff ? FileOwnerType.Staff : FileOwnerType.User, id)
            : throw new UnauthorizedException("Sign in to work with files.");

    // Someone else's file looks the same as a missing one.
    public static NotFoundException NotFound() => new("File not found.", "file.not_found");
}
