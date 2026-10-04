using System.Text.Json;
using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Common;
using HomeServices.Application.Errors;
using HomeServices.Application.Languages;
using HomeServices.Application.Messaging;
using HomeServices.Domain;
using HomeServices.Domain.Localization;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Translations;

public sealed record TranslationProgressDto(string Language, int Translated, int Missing);

/// <summary>A namespace (app) with how many keys it has and how far each language is.</summary>
public sealed record TranslationNamespaceDto(string Namespace, int KeyCount, IReadOnlyList<TranslationProgressDto> Languages);

/// <summary>One key with its text in every language that has one.</summary>
public sealed record TranslationRowDto(string Key, IReadOnlyDictionary<string, string> Values);

/// <summary>Keys an active language still lacks (the default language's keys are the full set).</summary>
public sealed record MissingTranslationsDto(string Namespace, string Language, int Total, int Translated, IReadOnlyList<string> MissingKeys);

/// <summary>What an import did. <see cref="Skipped"/>: keys the default language doesn't have; <see cref="Invalid"/>: bad keys or non-text values.</summary>
public sealed record TranslationImportResultDto(int Added, int Updated, int Unchanged, int Removed, IReadOnlyList<string> Skipped, IReadOnlyList<string> Invalid);

public sealed record GetTranslationNamespaces : IQuery<IReadOnlyList<TranslationNamespaceDto>>;

/// <summary>Keys of a namespace in key order. <see cref="MissingIn"/> keeps only keys without a text in that language.</summary>
public sealed record GetTranslations(
    string Namespace,
    string? Search = null,
    string? MissingIn = null,
    int Page = 1,
    int PageSize = GetTranslations.DefaultPageSize) : IQuery<PagedResult<TranslationRowDto>>
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 500;
}

/// <summary>
/// Creates a key or changes its texts. <see cref="Values"/> maps language codes to texts; an empty text removes that
/// language's text. A new key needs a default-language text, which can't be removed afterwards.
/// </summary>
public sealed record SaveTranslation(string Namespace, string Key, IReadOnlyDictionary<string, string?> Values) : ICommand<TranslationRowDto>;

/// <summary>Removes a key in every language.</summary>
public sealed record DeleteTranslationKey(string Namespace, string Key) : ICommand<bool>;

public sealed record GetMissingTranslations : IQuery<IReadOnlyList<MissingTranslationsDto>>;

/// <summary>A language's texts as an i18next JSON file. With <see cref="WithFallback"/>, missing texts come in the default language.</summary>
public sealed record ExportTranslations(string Namespace, string Language, bool WithFallback = false) : IQuery<string>;

/// <summary>
/// Loads an i18next JSON file (nested or dotted keys). Importing the default language creates keys; other languages only
/// get texts for existing keys. With <see cref="Replace"/>, texts (default language: keys) missing from the file are removed.
/// </summary>
public sealed record ImportTranslations(string Namespace, string Language, JsonElement Content, bool Replace = false) : ICommand<TranslationImportResultDto>;

public sealed class GetTranslationsValidator : AbstractValidator<GetTranslations>
{
    public GetTranslationsValidator()
    {
        RuleFor(x => x.Namespace).Must(UiTranslation.IsValidNamespace).WithErrorCode("namespace.invalid");
        RuleFor(x => x.Search).MaximumLength(100).WithErrorCode("search.too_long");
        RuleFor(x => x.MissingIn).Must(Language.IsValidCode).WithErrorCode("language.invalid").When(x => x.MissingIn is not null);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithErrorCode("page.invalid");
        RuleFor(x => x.PageSize).InclusiveBetween(1, GetTranslations.MaxPageSize).WithErrorCode("page_size.invalid");
    }
}

public sealed class SaveTranslationValidator : AbstractValidator<SaveTranslation>
{
    public SaveTranslationValidator(IAppDbContext db)
    {
        RuleFor(x => x.Namespace).Must(UiTranslation.IsValidNamespace).WithErrorCode("namespace.invalid");
        RuleFor(x => x.Key).Must(UiTranslation.IsValidKey).WithErrorCode("key.invalid");
        RuleFor(x => x.Values).NotEmpty().WithErrorCode("values.required");
        RuleFor(x => x.Values)
            .Must(values => values.Values.All(v => v is null || v.Length <= UiTranslation.ValueMaxLength)).WithErrorCode("values.too_long")
            .MustAsync(async (values, ct) =>
            {
                var codes = values.Keys.Select(k => k.Trim().ToLowerInvariant()).Distinct().ToList();
                return await db.Languages.CountAsync(l => codes.Contains(l.Code), ct) == codes.Count;
            })
            .WithErrorCode("values.language_invalid")
            .When(x => x.Values is { Count: > 0 });
    }
}

public sealed class ExportTranslationsValidator : AbstractValidator<ExportTranslations>
{
    public ExportTranslationsValidator()
    {
        RuleFor(x => x.Namespace).Must(UiTranslation.IsValidNamespace).WithErrorCode("namespace.invalid");
        RuleFor(x => x.Language).Must(Language.IsValidCode).WithErrorCode("language.invalid");
    }
}

public sealed class ImportTranslationsValidator : AbstractValidator<ImportTranslations>
{
    public ImportTranslationsValidator()
    {
        RuleFor(x => x.Namespace).Must(UiTranslation.IsValidNamespace).WithErrorCode("namespace.invalid");
        RuleFor(x => x.Language).Must(Language.IsValidCode).WithErrorCode("language.invalid");
        RuleFor(x => x.Content).Must(content => content.ValueKind == JsonValueKind.Object).WithErrorCode("content.invalid");
    }
}

public sealed class GetTranslationNamespacesHandler(IAppDbContext db, ILanguageCatalog catalog)
    : IQueryHandler<GetTranslationNamespaces, IReadOnlyList<TranslationNamespaceDto>>
{
    public async Task<IReadOnlyList<TranslationNamespaceDto>> HandleAsync(GetTranslationNamespaces query, CancellationToken cancellationToken)
    {
        var defaultCode = (await catalog.GetAsync(cancellationToken)).DefaultCode;
        var languages = await db.Languages.AsNoTracking().OrderBy(l => l.SortOrder).ThenBy(l => l.Code).Select(l => l.Code).ToListAsync(cancellationToken);
        var rows = await db.UiTranslations.AsNoTracking().Select(t => new { t.Namespace, t.Key, t.LanguageCode }).ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.Namespace)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g =>
            {
                var keys = g.Where(r => r.LanguageCode == defaultCode).Select(r => r.Key).ToHashSet(StringComparer.Ordinal);
                var progress = languages
                    .Select(code =>
                    {
                        var translated = g.Count(r => r.LanguageCode == code && keys.Contains(r.Key));
                        return new TranslationProgressDto(code, translated, keys.Count - translated);
                    })
                    .ToList();
                return new TranslationNamespaceDto(g.Key, keys.Count, progress);
            })
            .ToList();
    }
}

public sealed class GetTranslationsHandler(IAppDbContext db, ILanguageCatalog catalog) : IQueryHandler<GetTranslations, PagedResult<TranslationRowDto>>
{
    public async Task<PagedResult<TranslationRowDto>> HandleAsync(GetTranslations query, CancellationToken cancellationToken)
    {
        var defaultCode = (await catalog.GetAsync(cancellationToken)).DefaultCode;
        var rows = await TranslationRules.KeysAsync(db, UiTranslation.NormalizeNamespace(query.Namespace), defaultCode, cancellationToken);

        IEnumerable<TranslationRowDto> filtered = rows;
        if (query.MissingIn is { } missingIn)
        {
            var code = missingIn.Trim().ToLowerInvariant();
            filtered = filtered.Where(r => !r.Values.ContainsKey(code));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            filtered = filtered.Where(r =>
                r.Key.Contains(search, StringComparison.OrdinalIgnoreCase)
                || r.Values.Values.Any(v => v.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        var all = filtered.ToList();
        return new PagedResult<TranslationRowDto>(
            all.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList(),
            query.Page,
            query.PageSize,
            all.Count);
    }
}

public sealed class SaveTranslationHandler(IAppDbContext db, ILanguageCatalog catalog, UiTextsVersion version)
    : ICommandHandler<SaveTranslation, TranslationRowDto>
{
    public async Task<TranslationRowDto> HandleAsync(SaveTranslation command, CancellationToken cancellationToken)
    {
        var defaultCode = (await catalog.GetAsync(cancellationToken)).DefaultCode;
        var ns = UiTranslation.NormalizeNamespace(command.Namespace);
        var existing = await db.UiTranslations.Where(t => t.Namespace == ns && t.Key == command.Key).ToListAsync(cancellationToken);
        var values = command.Values.ToDictionary(v => v.Key.Trim().ToLowerInvariant(), v => v.Value);

        var defaultText = values.TryGetValue(defaultCode, out var given) ? given : existing.FirstOrDefault(t => t.LanguageCode == defaultCode)?.Value;
        if (string.IsNullOrEmpty(defaultText))
        {
            throw new DomainException("translation.default_required", "Every key needs a text in the default language.");
        }

        if (existing.Count == 0)
        {
            var keys = await db.UiTranslations.Where(t => t.Namespace == ns && t.LanguageCode == defaultCode).Select(t => t.Key).ToListAsync(cancellationToken);
            if (TranslationJson.Conflicts(command.Key, keys))
            {
                throw new ConflictException("This key would clash with an existing key (one can't be both a text and a group).", "translation.key_conflict");
            }
        }

        foreach (var (code, text) in values)
        {
            var row = existing.FirstOrDefault(t => t.LanguageCode == code);
            if (string.IsNullOrEmpty(text))
            {
                if (row is not null)
                {
                    db.UiTranslations.Remove(row);
                    existing.Remove(row);
                }
            }
            else if (row is null)
            {
                row = UiTranslation.Create(ns, command.Key, code, text);
                db.UiTranslations.Add(row);
                existing.Add(row);
            }
            else
            {
                row.ChangeValue(text);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        version.Bump();
        return new TranslationRowDto(command.Key, existing.ToDictionary(t => t.LanguageCode, t => t.Value));
    }
}

public sealed class DeleteTranslationKeyHandler(IAppDbContext db, UiTextsVersion version) : ICommandHandler<DeleteTranslationKey, bool>
{
    public async Task<bool> HandleAsync(DeleteTranslationKey command, CancellationToken cancellationToken)
    {
        var ns = UiTranslation.NormalizeNamespace(command.Namespace);
        var rows = await db.UiTranslations.Where(t => t.Namespace == ns && t.Key == command.Key).ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            throw new NotFoundException("No such key.", "translation.not_found");
        }

        db.UiTranslations.RemoveRange(rows);
        await db.SaveChangesAsync(cancellationToken);
        version.Bump();
        return true;
    }
}

public sealed class GetMissingTranslationsHandler(IAppDbContext db, ILanguageCatalog catalog)
    : IQueryHandler<GetMissingTranslations, IReadOnlyList<MissingTranslationsDto>>
{
    public async Task<IReadOnlyList<MissingTranslationsDto>> HandleAsync(GetMissingTranslations query, CancellationToken cancellationToken)
    {
        var languages = await catalog.GetAsync(cancellationToken);
        var rows = await db.UiTranslations.AsNoTracking().Select(t => new { t.Namespace, t.Key, t.LanguageCode }).ToListAsync(cancellationToken);
        var report = new List<MissingTranslationsDto>();

        foreach (var ns in rows.Select(r => r.Namespace).Distinct().Order(StringComparer.Ordinal))
        {
            var keys = rows.Where(r => r.Namespace == ns && r.LanguageCode == languages.DefaultCode).Select(r => r.Key).ToHashSet(StringComparer.Ordinal);
            foreach (var code in languages.ActiveCodes.Where(c => c != languages.DefaultCode))
            {
                var translated = rows.Where(r => r.Namespace == ns && r.LanguageCode == code).Select(r => r.Key).ToHashSet(StringComparer.Ordinal);
                var missing = keys.Where(k => !translated.Contains(k)).Order(StringComparer.Ordinal).ToList();
                report.Add(new MissingTranslationsDto(ns, code, keys.Count, keys.Count - missing.Count, missing));
            }
        }

        return report;
    }
}

public sealed class ExportTranslationsHandler(IAppDbContext db, ILanguageCatalog catalog) : IQueryHandler<ExportTranslations, string>
{
    public async Task<string> HandleAsync(ExportTranslations query, CancellationToken cancellationToken)
    {
        var code = await TranslationRules.EnsureLanguageAsync(db, query.Language, cancellationToken);
        var defaultCode = (await catalog.GetAsync(cancellationToken)).DefaultCode;
        var rows = await TranslationRules.KeysAsync(db, UiTranslation.NormalizeNamespace(query.Namespace), defaultCode, cancellationToken);

        var texts = rows
            .Select(r => (r.Key, Text: r.Values.GetValueOrDefault(code) ?? (query.WithFallback ? r.Values.GetValueOrDefault(defaultCode) : null)))
            .Where(r => r.Text is not null)
            .Select(r => KeyValuePair.Create(r.Key, r.Text!));
        return TranslationJson.Nest(texts);
    }
}

public sealed class ImportTranslationsHandler(IAppDbContext db, ILanguageCatalog catalog, UiTextsVersion version)
    : ICommandHandler<ImportTranslations, TranslationImportResultDto>
{
    public async Task<TranslationImportResultDto> HandleAsync(ImportTranslations command, CancellationToken cancellationToken)
    {
        var code = await TranslationRules.EnsureLanguageAsync(db, command.Language, cancellationToken);
        var defaultCode = (await catalog.GetAsync(cancellationToken)).DefaultCode;
        var ns = UiTranslation.NormalizeNamespace(command.Namespace);
        var file = TranslationJson.Flatten(command.Content);

        var all = await db.UiTranslations.Where(t => t.Namespace == ns).ToListAsync(cancellationToken);
        var defaultKeys = all.Where(t => t.LanguageCode == defaultCode).Select(t => t.Key).ToHashSet(StringComparer.Ordinal);
        var current = all.Where(t => t.LanguageCode == code).ToDictionary(t => t.Key, StringComparer.Ordinal);
        var isDefault = code == defaultCode;

        var invalid = file.Invalid.ToList();
        var skipped = new List<string>();
        int added = 0, updated = 0, unchanged = 0, removed = 0;

        foreach (var (key, text) in file.Texts)
        {
            if (current.TryGetValue(key, out var row))
            {
                if (row.Value == text)
                {
                    unchanged++;
                }
                else
                {
                    row.ChangeValue(text);
                    updated++;
                }

                continue;
            }

            if (!isDefault && !defaultKeys.Contains(key))
            {
                skipped.Add(key);
                continue;
            }

            if (isDefault && TranslationJson.Conflicts(key, defaultKeys))
            {
                invalid.Add(key);
                continue;
            }

            db.UiTranslations.Add(UiTranslation.Create(ns, key, code, text));
            defaultKeys.Add(key);
            added++;
        }

        if (command.Replace)
        {
            var gone = current.Keys.Where(k => !file.Texts.ContainsKey(k)).ToHashSet(StringComparer.Ordinal);

            // Removing a default-language key removes it in every language.
            var rowsToRemove = isDefault ? all.Where(t => gone.Contains(t.Key)).ToList() : gone.Select(k => current[k]).ToList();
            db.UiTranslations.RemoveRange(rowsToRemove);
            removed = gone.Count;
        }

        await db.SaveChangesAsync(cancellationToken);
        version.Bump();
        return new TranslationImportResultDto(added, updated, unchanged, removed, skipped.Order(StringComparer.Ordinal).ToList(), invalid);
    }
}

internal static class TranslationRules
{
    /// <summary>Every default-language key of a namespace with its texts, in key order.</summary>
    public static async Task<List<TranslationRowDto>> KeysAsync(IAppDbContext db, string ns, string defaultCode, CancellationToken cancellationToken)
    {
        var rows = await db.UiTranslations.AsNoTracking()
            .Where(t => t.Namespace == ns)
            .Select(t => new { t.Key, t.LanguageCode, t.Value })
            .ToListAsync(cancellationToken);
        var keys = rows.Where(r => r.LanguageCode == defaultCode).Select(r => r.Key).ToHashSet(StringComparer.Ordinal);

        return rows
            .Where(r => keys.Contains(r.Key))
            .GroupBy(r => r.Key)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new TranslationRowDto(g.Key, g.ToDictionary(r => r.LanguageCode, r => r.Value)))
            .ToList();
    }

    /// <summary>The code of an existing language (active or not), or 404.</summary>
    public static async Task<string> EnsureLanguageAsync(IAppDbContext db, string language, CancellationToken cancellationToken)
    {
        var code = language.Trim().ToLowerInvariant();
        return await db.Languages.AnyAsync(l => l.Code == code, cancellationToken)
            ? code
            : throw new NotFoundException($"'{language}' is not a language.", "language.not_found");
    }
}
