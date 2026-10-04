using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Errors;
using HomeServices.Application.Messaging;
using HomeServices.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Identity;

public sealed record GetMyProfile : IQuery<MyProfileDto>;

public sealed record UpdateMyProfile(string FullName, string? Email) : ICommand<MyProfileDto>;

public sealed class UpdateMyProfileValidator : AbstractValidator<UpdateMyProfile>
{
    public UpdateMyProfileValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithErrorCode("name.required")
            .MaximumLength(User.FullNameMaxLength).WithErrorCode("name.too_long");
        RuleFor(x => x.Email)
            .EmailAddress().WithErrorCode("email.invalid")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public sealed class GetMyProfileHandler(IAppDbContext db, ICurrentUser currentUser) : IQueryHandler<GetMyProfile, MyProfileDto>
{
    public async Task<MyProfileDto> HandleAsync(GetMyProfile query, CancellationToken cancellationToken) =>
        MyProfileDto.From(await CurrentUserLoader.LoadAsync(db, currentUser, cancellationToken));
}

public sealed class UpdateMyProfileHandler(IAppDbContext db, ICurrentUser currentUser) : ICommandHandler<UpdateMyProfile, MyProfileDto>
{
    public async Task<MyProfileDto> HandleAsync(UpdateMyProfile command, CancellationToken cancellationToken)
    {
        var user = await CurrentUserLoader.LoadAsync(db, currentUser, cancellationToken);
        user.UpdateProfile(command.FullName, command.Email);
        await db.SaveChangesAsync(cancellationToken);
        return MyProfileDto.From(user);
    }
}

internal static class CurrentUserLoader
{
    public static async Task<User> LoadAsync(IAppDbContext db, ICurrentUser currentUser, CancellationToken cancellationToken)
    {
        var user = Guid.TryParse(currentUser.UserId, out var id)
            ? await db.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken)
            : null;

        return user ?? throw new UnauthorizedException("Please sign in.");
    }
}
