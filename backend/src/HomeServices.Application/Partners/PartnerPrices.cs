using System.Globalization;
using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Catalog;
using HomeServices.Application.Messaging;
using HomeServices.Application.Notifications;
using HomeServices.Domain;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Localization;
using HomeServices.Domain.Notifications;
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
/// Saving confirms every price on the list (C3). A new or changed price more than
/// <see cref="PricingSettings.MaxPriceDeviation"/> times below the usual market price, or (labour only) above it, is
/// refused with "partner_price.unrealistic" (C4): usually a typo, and it would mislead customers.
/// </summary>
public sealed record SaveMyPrices(IReadOnlyList<MyPriceInput> Prices) : ICommand<MyPriceListDto>;

/// <summary>
/// The partner says their prices are still right: every price is confirmed now, so it counts again towards market
/// ranges and shows on their public profile (C3).
/// </summary>
public sealed record ConfirmMyPrices : ICommand<MyPriceListDto>;

/// <summary>
/// Tells partners whose prices are older than <see cref="PricingSettings.PriceFreshDays"/> days to check them: once
/// when they go out of date, then at most every <see cref="PricesReminder.RepeatDays"/> days. Returns how many were told.
/// </summary>
public sealed record RemindStalePrices : ICommand<int>;

/// <summary>One price: a single price (<see cref="PriceTo"/> null) or a range, labour per unit in AMD.</summary>
public sealed record MyPriceInput(Guid WorkItemId, int PriceFrom, int? PriceTo, bool IncludesMaterials);

/// <summary><see cref="FreshDays"/>: how long a price counts before it must be confirmed again.</summary>
public sealed record MyPriceListDto(IReadOnlyList<MyPriceItemDto> Items, int FreshDays)
{
    /// <summary>How many of the items have the partner's price.</summary>
    public int PricedCount => Items.Count(i => i.PriceFrom is not null);

    /// <summary>How many prices are out of date: they don't count and aren't shown until confirmed.</summary>
    public int StaleCount => Items.Count(i => i.IsStale);
}

/// <summary>
/// A work item on the partner's price list. Market prices are the usual labour range (null until priced); the partner's
/// own price is null until they set one; <see cref="ConfirmedAt"/> is when they last set or confirmed it, and
/// <see cref="IsStale"/> says it's too old to count.
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
    bool IncludesMaterials,
    DateTimeOffset? ConfirmedAt = null,
    bool IsStale = false);

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

public sealed class GetMyPricesHandler(
    IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language, IOptions<PricingSettings> settings, TimeProvider clock)
    : IQueryHandler<GetMyPrices, MyPriceListDto>
{
    public async Task<MyPriceListDto> HandleAsync(GetMyPrices query, CancellationToken cancellationToken)
    {
        var profile = await MyPartnerProfile.LoadAsync(db, currentUser, cancellationToken) ?? throw MyPartnerProfile.NotFound();
        return await MyPriceList.BuildAsync(db, language, profile, settings.Value, clock.GetUtcNow(), cancellationToken);
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
        await MyPriceList.CheckRealisticAsync(db, settings.Value, existing, command.Prices, cancellationToken);
        var now = clock.GetUtcNow();

        foreach (var (workItemId, price) in existing)
        {
            if (wanted.TryGetValue(workItemId, out var input))
            {
                price.Change(input.PriceFrom, input.PriceTo, input.IncludesMaterials);
                price.Confirm(now);
            }
            else
            {
                db.PartnerPrices.Remove(price);
            }
        }

        foreach (var input in command.Prices.Where(p => !existing.ContainsKey(p.WorkItemId)))
        {
            db.PartnerPrices.Add(PartnerPrice.Create(profile.Id, input.WorkItemId, input.PriceFrom, input.PriceTo, input.IncludesMaterials, now));
        }

        await db.SaveChangesAsync(cancellationToken);

        // The partner's prices count towards the market range of the items they offer.
        await MarketPrices.RecalculateAsync(db, settings.Value, clock, offered, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await MyPriceList.BuildAsync(db, language, profile, settings.Value, now, cancellationToken);
    }
}

public sealed class ConfirmMyPricesHandler(
    IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language, IOptions<PricingSettings> settings, TimeProvider clock)
    : ICommandHandler<ConfirmMyPrices, MyPriceListDto>
{
    public async Task<MyPriceListDto> HandleAsync(ConfirmMyPrices command, CancellationToken cancellationToken)
    {
        var profile = await MyPartnerProfile.LoadAsync(db, currentUser, cancellationToken) ?? throw MyPartnerProfile.NotFound();
        var now = clock.GetUtcNow();
        var prices = await db.PartnerPrices.Where(p => p.PartnerProfileId == profile.Id).ToListAsync(cancellationToken);
        var revived = prices.Where(p => p.IsStale(now, settings.Value.PriceFreshFor)).Select(p => p.WorkItemId).ToList();
        foreach (var price in prices)
        {
            price.Confirm(now);
        }

        await db.SaveChangesAsync(cancellationToken);
        if (revived.Count > 0)
        {
            // Prices that had stopped counting count again.
            await MarketPrices.RecalculateAsync(db, settings.Value, clock, revived, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        return await MyPriceList.BuildAsync(db, language, profile, settings.Value, now, cancellationToken);
    }
}

public sealed class RemindStalePricesHandler(IAppDbContext db, IOptions<PricingSettings> settings, TimeProvider clock)
    : ICommandHandler<RemindStalePrices, int>
{
    public async Task<int> HandleAsync(RemindStalePrices command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var staleBefore = now - settings.Value.PriceFreshFor;
        var repeatBefore = now.AddDays(-PricesReminder.RepeatDays);
        var stale = await db.PartnerPrices.AsNoTracking()
            .Where(p => p.ConfirmedAt < staleBefore)
            .GroupBy(p => p.PartnerProfileId)
            .Select(g => new { PartnerId = g.Key, Count = g.Count(), Newest = g.Max(p => p.ConfirmedAt) })
            .ToListAsync(cancellationToken);
        if (stale.Count == 0)
        {
            return 0;
        }

        var ids = stale.Select(s => s.PartnerId).ToList();
        var partners = await db.PartnerProfiles
            .Where(p => ids.Contains(p.Id) && p.Status == PartnerStatus.Approved)
            .ToListAsync(cancellationToken);
        var reminded = 0;
        foreach (var partner in partners)
        {
            var found = stale.Single(s => s.PartnerId == partner.Id);

            // Not told since the newest of these prices went out of date, or not for a while.
            var wentStale = found.Newest + settings.Value.PriceFreshFor;
            if (partner.PricesRemindedAt is { } last && last >= wentStale && last >= repeatBefore)
            {
                continue;
            }

            partner.MarkPricesReminded(now);
            Notifier.Add(
                db,
                partner.UserId,
                NotificationType.PricesNeedConfirming,
                PricesReminder.Link,
                new Dictionary<string, string?> { ["count"] = found.Count.ToString(CultureInfo.InvariantCulture) },
                now);
            reminded++;
        }

        if (reminded > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return reminded;
    }
}

public static class PricesReminder
{
    /// <summary>The Portal page of the partner's price list.</summary>
    public const string Link = "/prices";

    /// <summary>How often a partner who leaves prices out of date is told again.</summary>
    public const int RepeatDays = 30;
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

    /// <summary>
    /// Refuses new or changed prices far from the usual market price: more than <see cref="PricingSettings.MaxPriceDeviation"/>
    /// times below it, or above it for labour-only prices (materials can make a price much higher). Prices kept as they
    /// were pass, even when the market has moved since.
    /// </summary>
    public static async Task CheckRealisticAsync(
        IAppDbContext db,
        PricingSettings settings,
        IReadOnlyDictionary<Guid, PartnerPrice> existing,
        IEnumerable<MyPriceInput> prices,
        CancellationToken cancellationToken)
    {
        var changed = prices
            .Where(p => !existing.TryGetValue(p.WorkItemId, out var current)
                || current.PriceFrom != p.PriceFrom
                || current.PriceTo != (p.PriceTo == p.PriceFrom ? null : p.PriceTo)
                || current.IncludesMaterials != p.IncludesMaterials)
            .ToList();
        if (changed.Count == 0)
        {
            return;
        }

        var ids = changed.Select(p => p.WorkItemId).ToList();
        var typical = await db.WorkItems.AsNoTracking()
            .Where(w => ids.Contains(w.Id))
            .Select(w => new { w.Id, w.MarketTypical })
            .ToDictionaryAsync(w => w.Id, w => w.MarketTypical, cancellationToken);
        var limit = (long)settings.MaxPriceDeviation;
        foreach (var price in changed)
        {
            if (typical.GetValueOrDefault(price.WorkItemId) is not { } usual)
            {
                continue;
            }

            if (price.PriceFrom * limit < usual || (!price.IncludesMaterials && (price.PriceTo ?? price.PriceFrom) > usual * limit))
            {
                throw new DomainException(
                    "partner_price.unrealistic",
                    "A price is far from the usual price for this work. Check for a missing or extra zero.");
            }
        }
    }

    public static async Task<MyPriceListDto> BuildAsync(
        IAppDbContext db, ICurrentLanguage language, PartnerProfile profile, PricingSettings settings, DateTimeOffset now, CancellationToken cancellationToken)
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
                    mine?.IncludesMaterials ?? false,
                    mine?.ConfirmedAt,
                    mine?.IsStale(now, settings.PriceFreshFor) ?? false);
            })
            .ToList(),
            settings.PriceFreshDays);
    }
}
