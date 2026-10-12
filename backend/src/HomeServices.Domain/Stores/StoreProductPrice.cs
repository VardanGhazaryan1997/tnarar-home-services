using HomeServices.Domain.Common;

namespace HomeServices.Domain.Stores;

/// <summary>
/// One entry of a store product's price history: the regular price, the sale (if any) and when they were set. A row is
/// added when the product is created and whenever its price, sale price or sale end date changes.
/// </summary>
public sealed class StoreProductPrice : Entity
{
    private StoreProductPrice()
    {
    }

    public Guid StoreProductId { get; private set; }

    public int Price { get; private set; }

    public int? SalePrice { get; private set; }

    public DateOnly? SaleEndsOn { get; private set; }

    public DateTimeOffset RecordedAt { get; private set; }

    /// <summary>Records the product's current price as it is now.</summary>
    public static StoreProductPrice Record(StoreProduct product, DateTimeOffset recordedAt) =>
        new()
        {
            StoreProductId = product.Id,
            Price = product.Price,
            SalePrice = product.SalePrice,
            SaleEndsOn = product.SaleEndsOn,
            RecordedAt = recordedAt,
        };

    /// <summary>Records an earlier price of a product (used for demo data).</summary>
    public static StoreProductPrice Record(StoreProduct product, int price, DateTimeOffset recordedAt) =>
        new() { StoreProductId = product.Id, Price = price, RecordedAt = recordedAt };
}
