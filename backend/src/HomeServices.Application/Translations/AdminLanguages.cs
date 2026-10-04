using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Errors;
using HomeServices.Application.Languages;
using HomeServices.Application.Messaging;
using HomeServices.Domain.Localization;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Translations;

public sealed record AdminLanguageDto(string Code, string Name, string NativeName, bool IsActive, bool IsDefault, int SortOrder);

/// <summary>Every language, active or not, in switcher order.</summary>
public sealed record GetAdminLanguages : IQuery<IReadOnlyList<AdminLanguageDto>>;

/// <summary>The fields staff edit on a language.</summary>
public interface ILanguageFields
{
    string Name { get; }

    string NativeName { get; }

    int SortOrder { get; }
}

/// <summary>Adds a language. It starts inactive: translate the texts, then activate it.</summary>
public sealed record CreateLanguage(string Code, string Name, string NativeName, int SortOrder) : ICommand<AdminLanguageDto>, ILanguageFields;

public sealed record UpdateLanguage(string Code, string Name, string NativeName, int SortOrder) : ICommand<AdminLanguageDto>, ILanguageFields;

/// <summary>Shows or hides a language in the switcher. The default language can't be hidden.</summary>
public sealed record SetLanguageActive(string Code, bool IsActive) : ICommand<AdminLanguageDto>;

public abstract class LanguageFieldsValidator<T> : AbstractValidator<T>
    where T : ILanguageFields
{
    protected LanguageFieldsValidator()
    {
        RuleFor(x => x.Name)
            .Must(n => !string.IsNullOrWhiteSpace(n)).WithErrorCode("name.required")
            .Must(n => n is null || n.Trim().Length <= Language.NameMaxLength).WithErrorCode("name.too_long");
        RuleFor(x => x.NativeName)
            .Must(n => !string.IsNullOrWhiteSpace(n)).WithErrorCode("native_name.required")
            .Must(n => n is null || n.Trim().Length <= Language.NameMaxLength).WithErrorCode("native_name.too_long");
        RuleFor(x => x.SortOrder).InclusiveBetween(0, 10_000).WithErrorCode("sort_order.invalid");
    }
}

public sealed class CreateLanguageValidator : LanguageFieldsValidator<CreateLanguage>
{
    public CreateLanguageValidator() => RuleFor(x => x.Code).Must(Language.IsValidCode).WithErrorCode("code.invalid");
}

public sealed class UpdateLanguageValidator : LanguageFieldsValidator<UpdateLanguage>;

public sealed class GetAdminLanguagesHandler(IAppDbContext db) : IQueryHandler<GetAdminLanguages, IReadOnlyList<AdminLanguageDto>>
{
    public async Task<IReadOnlyList<AdminLanguageDto>> HandleAsync(GetAdminLanguages query, CancellationToken cancellationToken) =>
        (await db.Languages.AsNoTracking().OrderBy(l => l.SortOrder).ThenBy(l => l.Code).ToListAsync(cancellationToken))
            .Select(LanguageRules.ToDto)
            .ToList();
}

public sealed class CreateLanguageHandler(IAppDbContext db) : ICommandHandler<CreateLanguage, AdminLanguageDto>
{
    public async Task<AdminLanguageDto> HandleAsync(CreateLanguage command, CancellationToken cancellationToken)
    {
        var code = command.Code.Trim().ToLowerInvariant();
        if (await db.Languages.AnyAsync(l => l.Code == code, cancellationToken))
        {
            throw new ConflictException("This language already exists.", "language.code_taken");
        }

        var language = Language.Create(code, command.Name, command.NativeName, command.SortOrder);
        db.Languages.Add(language);
        await db.SaveChangesAsync(cancellationToken);
        return LanguageRules.ToDto(language);
    }
}

public sealed class UpdateLanguageHandler(IAppDbContext db, ILanguageCatalog catalog, UiTextsVersion version)
    : ICommandHandler<UpdateLanguage, AdminLanguageDto>
{
    public async Task<AdminLanguageDto> HandleAsync(UpdateLanguage command, CancellationToken cancellationToken)
    {
        var language = await LanguageRules.LoadAsync(db, command.Code, cancellationToken);
        language.Update(command.Name, command.NativeName, command.SortOrder);
        await db.SaveChangesAsync(cancellationToken);
        catalog.Invalidate();
        version.Bump();
        return LanguageRules.ToDto(language);
    }
}

public sealed class SetLanguageActiveHandler(IAppDbContext db, ILanguageCatalog catalog, UiTextsVersion version)
    : ICommandHandler<SetLanguageActive, AdminLanguageDto>
{
    public async Task<AdminLanguageDto> HandleAsync(SetLanguageActive command, CancellationToken cancellationToken)
    {
        var language = await LanguageRules.LoadAsync(db, command.Code, cancellationToken);
        if (command.IsActive)
        {
            language.Activate();
        }
        else
        {
            language.Deactivate();
        }

        await db.SaveChangesAsync(cancellationToken);
        catalog.Invalidate();
        version.Bump();
        return LanguageRules.ToDto(language);
    }
}

internal static class LanguageRules
{
    public static async Task<Language> LoadAsync(IAppDbContext db, string code, CancellationToken cancellationToken)
    {
        var normalized = code.Trim().ToLowerInvariant();
        return await db.Languages.SingleOrDefaultAsync(l => l.Code == normalized, cancellationToken)
            ?? throw new NotFoundException($"'{code}' is not a language.", "language.not_found");
    }

    public static AdminLanguageDto ToDto(Language language) =>
        new(language.Code, language.Name, language.NativeName, language.IsActive, language.IsDefault, language.SortOrder);
}
