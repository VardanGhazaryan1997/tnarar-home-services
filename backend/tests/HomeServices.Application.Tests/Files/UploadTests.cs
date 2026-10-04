using HomeServices.Application.Abstractions;
using HomeServices.Application.Errors;
using HomeServices.Application.Files;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain;
using HomeServices.Domain.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Tests.Files;

public class UploadTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserId = Guid.CreateVersion7();
    private static readonly Guid OtherUserId = Guid.CreateVersion7();
    private static readonly Guid StaffId = Guid.CreateVersion7();

    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();
    private readonly FakeFileStorage _storage = new();
    private readonly FakeImageProcessor _images = new();
    private readonly FakeClock _clock = new(Now);
    private readonly FileSettings _settings = new();

    private static ICurrentUser User(Guid? id = null) => new FakeCurrentUser(id ?? UserId);

    private static ICurrentUser Staff() => new FakeCurrentUser(StaffId, isStaff: true);

    private FileDtoFactory Dtos() => new(_storage, Options.Create(_settings), _clock);

    private Task<UploadTicket> RequestAsync(RequestUpload command, ICurrentUser? user = null) =>
        new RequestUploadHandler(_db, user ?? User(), _storage, Options.Create(_settings), _clock)
            .HandleAsync(command, CancellationToken.None);

    private Task<FileDto> CompleteAsync(Guid fileId, ICurrentUser? user = null) =>
        new CompleteUploadHandler(_db, user ?? User(), _storage, _images, Dtos(), _clock)
            .HandleAsync(new CompleteUpload(fileId), CancellationToken.None);

    private Task<FileDto> GetAsync(Guid fileId, ICurrentUser? user = null) =>
        new GetFileHandler(_db, user ?? User(), Dtos()).HandleAsync(new GetFile(fileId), CancellationToken.None);

    private async Task<StoredFile> GivenUploaded(string contentType, byte[] content, ICurrentUser? user = null)
    {
        var ticket = await RequestAsync(new RequestUpload("upload", contentType, content.Length), user);
        var file = await _db.Files.SingleAsync(f => f.Id == ticket.FileId);
        _storage.Upload(file.Key, content, contentType);
        return file;
    }

    private async Task<StoredFile> ReloadAsync(Guid id)
    {
        _db.ChangeTracker.Clear();
        return await _db.Files.SingleAsync(f => f.Id == id);
    }

    [Fact]
    public async Task Requesting_an_upload_records_a_pending_file_and_returns_a_signed_PUT_link()
    {
        var ticket = await RequestAsync(new RequestUpload("kitchen.jpg", "image/jpeg", 2048));

        var file = await _db.Files.SingleAsync();
        file.Id.ShouldBe(ticket.FileId);
        file.Status.ShouldBe(FileStatus.Pending);
        file.OwnerType.ShouldBe(FileOwnerType.User);
        file.OwnerId.ShouldBe(UserId.ToString());
        file.OriginalFileName.ShouldBe("kitchen.jpg");

        ticket.Method.ShouldBe("PUT");
        ticket.UploadUrl.ShouldStartWith($"https://storage.test/{file.Key}?put&type=image%2Fjpeg");
        ticket.Headers.ShouldBe(new Dictionary<string, string> { ["Content-Type"] = "image/jpeg" });
        ticket.ExpiresAt.ShouldBe(Now.AddMinutes(15));
    }

    [Fact]
    public async Task Staff_uploads_belong_to_the_staff_member()
    {
        await RequestAsync(new RequestUpload("contract.pdf", "application/pdf", 10), Staff());

        var file = await _db.Files.SingleAsync();
        file.OwnerType.ShouldBe(FileOwnerType.Staff);
        file.OwnerId.ShouldBe(StaffId.ToString());
    }

    [Fact]
    public async Task Signed_out_requests_are_refused()
    {
        var anonymous = new FakeCurrentUser(null);

        await Should.ThrowAsync<UnauthorizedException>(() => RequestAsync(new RequestUpload("a.jpg", "image/jpeg", 1), anonymous));
        await Should.ThrowAsync<UnauthorizedException>(() => CompleteAsync(Guid.NewGuid(), anonymous));
        await Should.ThrowAsync<UnauthorizedException>(() => GetAsync(Guid.NewGuid(), anonymous));
    }

    [Fact]
    public async Task One_person_can_start_only_so_many_uploads_per_hour()
    {
        _settings.MaxUploadsPerHour = 2;
        GivenEarlierUpload(UserId, Now.AddMinutes(-10));
        GivenEarlierUpload(UserId, Now.AddMinutes(-90)); // more than an hour ago
        GivenEarlierUpload(OtherUserId, Now.AddMinutes(-5)); // someone else
        await _db.SaveChangesAsync();

        await RequestAsync(new RequestUpload("a.jpg", "image/jpeg", 1));
        _db.Files.Local.Single(f => f.CreatedAt == default).MarkCreated(Now, null);
        await _db.SaveChangesAsync();

        var error = await Should.ThrowAsync<TooManyRequestsException>(() => RequestAsync(new RequestUpload("b.jpg", "image/jpeg", 1)));
        error.Code.ShouldBe("file.too_many_uploads");
    }

    private void GivenEarlierUpload(Guid ownerId, DateTimeOffset at)
    {
        var file = StoredFile.Begin(FileOwnerType.User, ownerId.ToString(), "old.jpg", "image/jpeg", 1, at);
        file.MarkCreated(at, null);
        _db.Files.Add(file);
    }

    [Fact]
    public async Task Completing_an_image_makes_a_thumbnail_and_returns_signed_download_links()
    {
        var file = await GivenUploaded("image/jpeg", SampleFiles.Jpeg);

        var dto = await CompleteAsync(file.Id);

        var thumbnailKey = file.ThumbnailKeyFor();
        _storage.Objects[thumbnailKey].Content.ShouldBe(FakeImageProcessor.Thumbnail);
        _storage.Objects[thumbnailKey].ContentType.ShouldBe("image/jpeg");
        _images.LastMaxSize.ShouldBe(StoredFile.ThumbnailMaxSize);

        dto.Status.ShouldBe("Ready");
        dto.Kind.ShouldBe("Image");
        dto.Size.ShouldBe(SampleFiles.Jpeg.Length);
        dto.Width.ShouldBe(4000);
        dto.Height.ShouldBe(3000);
        dto.Url.ShouldBe($"https://storage.test/{file.Key}?get&expires={Now.AddMinutes(60).ToUnixTimeSeconds()}");
        dto.ThumbnailUrl.ShouldBe($"https://storage.test/{thumbnailKey}?get&expires={Now.AddMinutes(60).ToUnixTimeSeconds()}");
        dto.UrlExpiresAt.ShouldBe(Now.AddMinutes(60));

        var saved = await ReloadAsync(file.Id);
        saved.Status.ShouldBe(FileStatus.Ready);
        saved.ThumbnailKey.ShouldBe(thumbnailKey);
        saved.CompletedAt.ShouldBe(Now);
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("video/mp4")]
    public async Task Completing_a_document_or_video_needs_no_thumbnail(string contentType)
    {
        var content = contentType == "application/pdf" ? SampleFiles.Pdf : SampleFiles.Mp4;
        var file = await GivenUploaded(contentType, content);

        var dto = await CompleteAsync(file.Id);

        dto.Status.ShouldBe("Ready");
        dto.Url.ShouldNotBeNull();
        dto.ThumbnailUrl.ShouldBeNull();
        dto.Width.ShouldBeNull();
        _images.LastMaxSize.ShouldBeNull();
    }

    [Fact]
    public async Task Completing_before_the_file_is_uploaded_can_be_retried()
    {
        var ticket = await RequestAsync(new RequestUpload("a.jpg", "image/jpeg", 100));

        var error = await Should.ThrowAsync<DomainException>(() => CompleteAsync(ticket.FileId));

        error.Code.ShouldBe("file.not_uploaded");
        (await ReloadAsync(ticket.FileId)).Status.ShouldBe(FileStatus.Pending);
    }

    [Fact]
    public async Task A_file_larger_than_allowed_is_rejected_and_deleted()
    {
        var content = new byte[StoredFile.MaxSizeFor(FileKind.Image) + 1];
        SampleFiles.Jpeg.CopyTo(content, 0);
        var ticket = await RequestAsync(new RequestUpload("big.jpg", "image/jpeg", 1000)); // the browser claimed less
        var file = await _db.Files.SingleAsync(f => f.Id == ticket.FileId);
        _storage.Upload(file.Key, content);

        var error = await Should.ThrowAsync<DomainException>(() => CompleteAsync(file.Id));

        error.Code.ShouldBe("file.too_large");
        _storage.Objects.ShouldNotContainKey(file.Key);
        var saved = await ReloadAsync(file.Id);
        saved.Status.ShouldBe(FileStatus.Rejected);
        saved.RejectionCode.ShouldBe("file.too_large");
    }

    [Fact]
    public async Task A_program_uploaded_as_a_photo_is_rejected_and_deleted()
    {
        var file = await GivenUploaded("image/jpeg", SampleFiles.Executable);

        var error = await Should.ThrowAsync<DomainException>(() => CompleteAsync(file.Id));

        error.Code.ShouldBe("file.invalid_content");
        _storage.Deleted.ShouldBe(new[] { file.Key });
        (await ReloadAsync(file.Id)).Status.ShouldBe(FileStatus.Rejected);
    }

    [Fact]
    public async Task An_image_that_cannot_be_read_is_rejected()
    {
        _images.Readable = false;
        var file = await GivenUploaded("image/png", SampleFiles.Png);

        var error = await Should.ThrowAsync<DomainException>(() => CompleteAsync(file.Id));

        error.Code.ShouldBe("file.invalid_content");
        _storage.Objects.ShouldNotContainKey(file.ThumbnailKeyFor());
        (await ReloadAsync(file.Id)).Status.ShouldBe(FileStatus.Rejected);
    }

    [Fact]
    public async Task Completing_again_returns_the_ready_file_and_repeats_the_rejection()
    {
        var ready = await GivenUploaded("application/pdf", SampleFiles.Pdf);
        await CompleteAsync(ready.Id);
        var rejected = await GivenUploaded("application/pdf", SampleFiles.Executable);
        await Should.ThrowAsync<DomainException>(() => CompleteAsync(rejected.Id));

        (await CompleteAsync(ready.Id)).Status.ShouldBe("Ready");
        (await Should.ThrowAsync<DomainException>(() => CompleteAsync(rejected.Id))).Code.ShouldBe("file.invalid_content");
    }

    [Fact]
    public async Task Only_the_owner_can_complete_an_upload()
    {
        var file = await GivenUploaded("application/pdf", SampleFiles.Pdf);

        (await Should.ThrowAsync<NotFoundException>(() => CompleteAsync(file.Id, User(OtherUserId)))).Code.ShouldBe("file.not_found");
        (await Should.ThrowAsync<NotFoundException>(() => CompleteAsync(file.Id, Staff()))).Code.ShouldBe("file.not_found");
        (await Should.ThrowAsync<NotFoundException>(() => CompleteAsync(Guid.NewGuid()))).Code.ShouldBe("file.not_found");
        (await ReloadAsync(file.Id)).Status.ShouldBe(FileStatus.Pending);
    }

    [Fact]
    public async Task The_owner_and_staff_can_see_a_file_but_other_users_cannot()
    {
        var file = await GivenUploaded("application/pdf", SampleFiles.Pdf);
        await CompleteAsync(file.Id);

        var mine = await GetAsync(file.Id);
        mine.Id.ShouldBe(file.Id);
        mine.FileName.ShouldBe("upload");
        mine.ContentType.ShouldBe("application/pdf");
        mine.Url.ShouldNotBeNull();
        (await GetAsync(file.Id, Staff())).Url.ShouldNotBeNull();
        (await Should.ThrowAsync<NotFoundException>(() => GetAsync(file.Id, User(OtherUserId)))).Code.ShouldBe("file.not_found");
        (await Should.ThrowAsync<NotFoundException>(() => GetAsync(Guid.NewGuid()))).Code.ShouldBe("file.not_found");
    }

    [Fact]
    public async Task A_pending_file_has_no_download_links_yet()
    {
        var ticket = await RequestAsync(new RequestUpload("a.jpg", "image/jpeg", 100));

        var dto = await GetAsync(ticket.FileId);

        dto.Status.ShouldBe("Pending");
        dto.Url.ShouldBeNull();
        dto.ThumbnailUrl.ShouldBeNull();
        dto.UrlExpiresAt.ShouldBeNull();
    }
}
