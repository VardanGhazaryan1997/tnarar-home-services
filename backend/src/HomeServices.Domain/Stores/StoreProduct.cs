using HomeServices.Domain.Catalog;
using HomeServices.Domain.Common;

namespace HomeServices.Domain.Stores;

/// <summary>
/// A product a store sells, as the store calls it. Once linked to one of our <see cref="Material"/>s (with a quality
/// grade) it is compared with other stores' products per unit of that material: the price of a pack divided by the
/// <see cref="PackSize"/> in the material's unit. Unlinked products are visible only in the Back Office.
/// </summary>
public sealed class StoreProduct : AuditableEntity, IAudited
{
    public const int MaxNameLength = 200;
    public const int MaxBrandLength = 100;
    public const int MaxCodeLength = 64;
    public const int MaxPackLabelLength = 50;
    public const decimal MaxPackSize = 9_999_999m;

    private StoreProduct()
    {
    }

    public Guid StoreId { get; private set; }

    /// <summary>The material it is compared under; null until staff link it.</summary>
    public Guid? MaterialId { get; private set; }

    /// <summary>Budget, standard or premium; set whenever the product is linked to a material.</summary>
    public MaterialGrade? Grade { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Brand { get; private set; }

    /// <summary>The store's own product code (SKU); unique within the store.</summary>
    public string? Code { get; private set; }

    /// <summary>How much of the material's unit one pack holds, e.g. 25 for a 25 kg bag of a per-kg material.</summary>
    public decimal PackSize { get; private set; }

    /// <summary>The pack as customers know it, e.g. "25 kg bag".</summary>
    public string? PackLabel { get; private set; }

    /// <summary>The price of one pack in AMD.</summary>
    public int Price { get; private set; }

    public int? SalePrice { get; private set; }

    /// <summary>The last day the sale price applies; set exactly when there is a sale price.</summary>
    public DateOnly? SaleEndsOn { get; private set; }

    public bool InStock { get; private set; }

    public Guid? PhotoFileId { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>When the price was set or last checked; prices not checked for a while are "stale" and stop counting.</summary>
    public DateTimeOffset PriceUpdatedAt { get; private set; }

    public static StoreProduct Create(
        Guid storeId,
        string name,
        string? brand,
        string? code,
        decimal packSize,
        string? packLabel,
        int price,
        int? salePrice,
        DateOnly? saleEndsOn,
        bool inStock,
        Guid? photoFileId,
        DateTimeOffset now)
    {
        var product = new StoreProduct { StoreId = storeId, IsActive = true, PriceUpdatedAt = now };
        product.Update(name, brand, code, packSize, packLabel, price, salePrice, saleEndsOn, inStock, photoFileId, now);
        return product;
    }

    /// <summary>
    /// Changes every edited field at once; the price counts as checked <paramref name="now"/> when the price, sale price
    /// or sale end date changed. The caller checks that the code is free and that the photo exists.
    /// </summary>
    public void Update(
        string name,
        string? brand,
        string? code,
        decimal packSize,
        string? packLabel,
        int price,
        int? salePrice,
        DateOnly? saleEndsOn,
        bool inStock,
        Guid? photoFileId,
        DateTimeOffset now)
    {
        var cleanName = Store.Clean(name);
        var cleanBrand = Store.Clean(brand);
        var cleanCode = Store.Clean(code);
        var cleanLabel = Store.Clean(packLabel);
        if (cleanName is null or { Length: > MaxNameLength }
            || cleanBrand is { Length: > MaxBrandLength }
            || cleanCode is { Length: > MaxCodeLength }
            || cleanLabel is { Length: > MaxPackLabelLength }
            || packSize is <= 0m or > MaxPackSize
            || price is < 1 or > Material.PriceLimit)
        {
            throw new DomainException(
                "store_product.invalid",
                $"A product needs a name (up to {MaxNameLength}), a pack size above 0 and a price of 1–{Material.PriceLimit} AMD; brand is up to {MaxBrandLength}, code up to {MaxCodeLength} and pack label up to {MaxPackLabelLength} characters.");
        }

        if ((salePrice is null) != (saleEndsOn is null) || (salePrice is { } sale && (sale < 1 || sale >= price)))
        {
            throw new DomainException("store_product.sale_invalid", "A sale needs both a price below the usual price and the last day it applies.");
        }

        if (price != Price || salePrice != SalePrice || saleEndsOn != SaleEndsOn)
        {
            PriceUpdatedAt = now;
        }

        Name = cleanName;
        Brand = cleanBrand;
        Code = cleanCode;
        PackSize = packSize;
        PackLabel = cleanLabel;
        Price = price;
        SalePrice = salePrice;
        SaleEndsOn = saleEndsOn;
        InStock = inStock;
        PhotoFileId = photoFileId;
    }

    /// <summary>Compares the product under a material, as a budget, standard or premium product. The caller checks the material exists.</summary>
    public void LinkTo(Guid materialId, MaterialGrade? grade)
    {
        if (grade is null)
        {
            throw new DomainException("store_product.grade_required", "A product linked to a material needs a grade (budget, standard or premium).");
        }

        if (materialId == Guid.Empty || !Enum.IsDefined(grade.Value))
        {
            throw new DomainException("store_product.invalid", "Unknown material or grade.");
        }

        MaterialId = materialId;
        Grade = grade;
    }

    /// <summary>Stops comparing the product: it is visible only in the Back Office again.</summary>
    public void Unlink()
    {
        MaterialId = null;
        Grade = null;
    }

    /// <summary>Staff checked the store's price and it is still right.</summary>
    public void ConfirmPrice(DateTimeOffset now) => PriceUpdatedAt = now;

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    /// <summary>The price of one pack today: the sale price up to and including the sale's last day, otherwise the usual price.</summary>
    public int CurrentPrice(DateOnly today) => SalePrice is { } sale && today <= SaleEndsOn ? sale : Price;

    /// <summary>The price of one unit of the material today (AMD, with decimals).</summary>
    public decimal UnitPrice(DateOnly today) => CurrentPrice(today) / PackSize;

    /// <summary>True when the price wasn't set or checked within <paramref name="freshFor"/>.</summary>
    public bool IsStale(DateTimeOffset now, TimeSpan freshFor) => PriceUpdatedAt < now - freshFor;
}
