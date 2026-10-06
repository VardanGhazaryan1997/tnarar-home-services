using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Catalog;
using HomeServices.Application.Messaging;
using HomeServices.Domain;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Localization;
using HomeServices.Domain.Partners;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Partners;

/// <summary>
/// The signed-in partner's price list: every active work item in the categories they offer (a main category covers
/// its subcategories), in catalog order, with the market range and the partner's own price when set.
/// </summary>
public sealed record GetMyPrices : IQuery<MyPriceListDto>;

/// <summary>
/// Saves the partner's price list. The list sent replaces the prices of the items they offer: items left out lose
/// their price. Prices of items they no longer offer are kept (they come back if the service is offered again).
/// </summary>
public sealed record SaveMyPrices(IReadOnlyList<MyPriceInput> Prices) : ICommand<MyPriceListDto>;

/// <summary>One price: a single price (<see cref="PriceTo"/> null) or a range, labour per unit in AMD.</summary>
public sealed record MyPriceInput(Guid WorkItemId, int PriceFrom, int? PriceTo, bool IncludesMaterials);

public sealed record MyPriceListDto(IReadOnlyList<MyPriceItemDto> Items)
{
    /// <summary>How many of the items have the partner's price.</summary>
    public int PricedCount => Items.Count(i => i.PriceFrom is not null);
}

/// <summary>
/// A work item on the partner's price list. Market prices are the usual labour range (null until priced); the partner's
/// own price is null until they set one.
/// </summary>
public sealed record MyPriceItemDto(
    Guid WorkItemId,
    string Slug,
    string Name,
    WorkUnit Unit,
    Guid CategoryId,
    string CategoryName,
    Guid MainCategoryId,
    string MainCategoryName,
    int? MarketMin,
    int? MarketTypical,
    int? MarketMax,
    int? PriceFrom,
    int? PriceTo,
    bool IncludesMaterials);

public sealed class SaveMyPricesValidator : AbstractValidator<SaveMyPrices>
{
    /// <summary>More than every work item there is; stops oversized requests.</summary>
    public const int MaxPrices = 1000;

    public SaveMyPricesValidator()
    {
        RuleFor(x => x.Prices).NotNull().WithErrorCode("prices.required")
            .Must(prices => prices is null || prices.Count <= MaxPrices).WithErrorCode("prices.too_many")
            .Must(prices => prices is null || prices.Select(p => p.WorkItemId).Distinct().Count() == prices.Count)
            .WithErrorCode("prices.duplicate");
        RuleForEach(x => x.Prices)
            .Must(p => PartnerPrice.IsValid(p.PriceFrom, p.PriceTo))
            .WithErrorCode("partner_price.invalid");
    }
}

public sealed class GetMyPricesHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language)
    : IQueryHandler<GetMyPrices, MyPriceListDto>
{
    public async Task<MyPriceListDto> HandleAsync(GetMyPrices query, CancellationToken cancellationToken)
    {
        var profile = await MyPartnerProfile.LoadAsync(db, currentUser, cancellationToken) ?? throw MyPartnerProfile.NotFound();
        return await MyPriceList.BuildAsync(db, language, profile, cancellationToken);
    }
}

public sealed class SaveMyPricesHandler(
    IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language, IOptions<PricingSettings> settings, TimeProvider clock)
    : ICommandHandler<SaveMyPrices, MyPriceListDto>
{
    public async Task<MyPriceListDto> HandleAsync(SaveMyPrices command, CancellationToken cancellationToken)
    {
        var profile = await MyPartnerProfile.LoadAsync(db, currentUser, cancellationToken) ?? throw MyPartnerProfile.NotFound();
        var offered = (await MyPriceList.OfferedItemsAsync(db, profile, cancellationToken)).Select(i => i.Item.Id).ToHashSet();
        if (command.Prices.Any(p => !offered.Contains(p.WorkItemId)))
        {
            throw new DomainException("partner_price.not_offered", "You can only price work in the services you offer.");
        }

        var existing = await db.PartnerPrices
            .Where(p => p.PartnerProfileId == profile.Id && offered.Contains(p.WorkItemId))
            .ToDictionaryAsync(p => p.WorkItemId, cancellationToken);
        var wanted = command.Prices.ToDictionary(p => p.WorkItemId);

        foreach (var (workItemId, price) in existing)
        {
            if (wanted.TryGetValue(workItemId, out var input))
            {
                price.Change(input.PriceFrom, input.PriceTo, input.IncludesMaterials);
            }
            else
            {
                db.PartnerPrices.Remove(price);
            }
        }

        foreach (var input in command.Prices.Where(p => !existing.ContainsKey(p.WorkItemId)))
        {
            db.PartnerPrices.Add(PartnerPrice.Create(profile.Id, input.WorkItemId, input.PriceFrom, input.PriceTo, input.IncludesMaterials));
        }

        await db.SaveChangesAsync(cancellationToken);

        // The partner's prices count towards the market range of the items they offer.
        await MarketPrices.RecalculateAsync(db, settings.Value, clock, offered, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await MyPriceList.BuildAsync(db, language, profile, cancellationToken);
    }
}

internal static class MyPriceList
{
    public sealed record OfferedItem(WorkItem Item, Category Subcategory, Category Main);

    /// <summary>Active work items in visible categories the partner offers, in catalog order.</summary>
    public static async Task<List<OfferedItem>> OfferedItemsAsync(IAppDbContext db, PartnerProfile profile, CancellationToken cancellationToken)
    {
        var serviceIds = profile.Services.Select(s => s.CategoryId).ToHashSet();
        var categories = await db.Categories.AsNoTracking().Where(c => c.IsActive).ToListAsync(cancellationToken);
        var byId = categories.ToDictionary(c => c.Id);
        var subcategories = categories
            .Where(c => c.ParentId is { } parentId && byId.ContainsKey(parentId) && (serviceIds.Contains(c.Id) || serviceIds.Contains(parentId)))
            .Select(c => c.Id)
            .ToList();
        if (subcategories.Count == 0)
        {
            return [];
        }

        var order = WorkItemCatalog.Order(categories);
        var items = await db.WorkItems.AsNoTracking()
            .Where(w => w.IsActive && subcategories.Contains(w.CategoryId))
            .ToListAsync(cancellationToken);

        return WorkItemCatalog.Sort(items, order)
            .Select(w => new OfferedItem(w, byId[w.CategoryId], byId[byId[w.CategoryId].ParentId!.Value]))
            .ToList();
    }

    public static async Task<MyPriceListDto> BuildAsync(IAppDbContext db, ICurrentLanguage language, PartnerProfile profile, CancellationToken cancellationToken)
    {
        var offered = await OfferedItemsAsync(db, profile, cancellationToken);
        var prices = await db.PartnerPrices.AsNoTracking()
            .Where(p => p.PartnerProfileId == profile.Id)
            .ToDictionaryAsync(p => p.WorkItemId, cancellationToken);

        string Name(LocalizedText text) => text.Get(language.Code, language.DefaultCode);

        return new MyPriceListDto(offered
            .Select(o =>
            {
                var mine = prices.GetValueOrDefault(o.Item.Id);
                return new MyPriceItemDto(
                    o.Item.Id,
                    o.Item.Slug,
                    Name(o.Item.Name),
                    o.Item.Unit,
                    o.Subcategory.Id,
                    Name(o.Subcategory.Name),
                    o.Main.Id,
                    Name(o.Main.Name),
                    o.Item.MarketMin,
                    o.Item.MarketTypical,
                    o.Item.MarketMax,
                    mine?.PriceFrom,
                    mine?.PriceTo,
                    mine?.IncludesMaterials ?? false);
            })
            .ToList());
    }
}
