using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Errors;
using HomeServices.Application.Files;
using HomeServices.Application.Messaging;
using HomeServices.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Stores;

/// <summary>
/// Quick price editing (R7): the price, sale and stock of several of a store's products at once. <c>Confirm</c> marks
/// an unchanged price as checked today. All or nothing: when one product's prices are invalid, nothing is saved.
/// </summary>
public sealed record SaveStoreProductPrices(Guid StoreId, IReadOnlyList<StoreProductPriceInput> Items) : ICommand<IReadOnlyList<AdminStoreProductDto>>
{
    public const int MaxItems = 100;
}

public sealed record StoreProductPriceInput(Guid ProductId, int Price, int? SalePrice, DateOnly? SaleEndsOn, bool InStock, bool Confirm = false);

public sealed class SaveStoreProductPricesValidator : AbstractValidator<SaveStoreProductPrices>
{
    public SaveStoreProductPricesValidator()
    {
        RuleFor(x => x.Items)
            .Must(items => items is { Count: >= 1 and <= SaveStoreProductPrices.MaxItems })
            .WithErrorCode("items.count_invalid");
        RuleFor(x => x.Items)
            .Must(items => items.Select(i => i.ProductId).Distinct().Count() == items.Count)
            .When(x => x.Items is not null)
            .WithErrorCode("items.duplicate");
    }
}

public sealed class SaveStoreProductPricesHandler(
    IAppDbContext db, FileDtoFactory files, ICurrentLanguage language, IOptions<StorePricingSettings> settings, TimeProvider clock)
    : ICommandHandler<SaveStoreProductPrices, IReadOnlyList<AdminStoreProductDto>>
{
    public async Task<IReadOnlyList<AdminStoreProductDto>> HandleAsync(SaveStoreProductPrices command, CancellationToken cancellationToken)
    {
        var ids = command.Items.Select(i => i.ProductId).ToList();
        var products = await db.StoreProducts
            .Where(p => p.StoreId == command.StoreId && ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);
        if (products.Count != ids.Count)
        {
            throw new NotFoundException("A product does not exist in this store.", "store_product.not_found");
        }

        var now = clock.GetUtcNow();
        foreach (var item in command.Items)
        {
            var product = products[item.ProductId];
            var changed = product.PriceDiffers(item.Price, item.SalePrice, item.SaleEndsOn);
            try
            {
                product.ChangePrice(item.Price, item.SalePrice, item.SaleEndsOn, item.InStock, now);
            }
            catch (DomainException error)
            {
                // Nothing is saved: the products changed so far are only changed in memory.
                throw new DomainException(error.Code, $"{product.Name}: {error.Message}");
            }

            if (changed)
            {
                StorePriceHistory.Record(db, product, now);
            }
            else if (item.Confirm)
            {
                product.ConfirmPrice(now);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await StoreData.RefreshMaterialPricesAsync(db, settings.Value, clock, products.Values.Select(p => p.MaterialId), cancellationToken);
        var saved = ids.Select(id => products[id]).ToList();
        return await StoreData.ProductDtosAsync(db, files, language, settings.Value, now, saved, cancellationToken);
    }
}
