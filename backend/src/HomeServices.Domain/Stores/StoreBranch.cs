using HomeServices.Domain.Common;

namespace HomeServices.Domain.Stores;

/// <summary>A place where customers can buy from a <see cref="Store"/>. Created and replaced through the store.</summary>
public sealed class StoreBranch : Entity, IAudited
{
    public const int MaxNameLength = 100;
    public const int MaxAddressLength = 200;
    public const int MaxPhoneLength = 30;
    public const int MaxOpeningHoursLength = 200;

    private StoreBranch()
    {
    }

    public Guid StoreId { get; private set; }

    /// <summary>What the branch is called, e.g. "Komitas".</summary>
    public string Name { get; private set; } = string.Empty;

    public Guid CityId { get; private set; }

    public Guid? DistrictId { get; private set; }

    public string Address { get; private set; } = string.Empty;

    public string? Phone { get; private set; }

    /// <summary>Free text, e.g. "Mon–Sat 9:00–19:00".</summary>
    public string? OpeningHours { get; private set; }

    public int SortOrder { get; private set; }

    internal static StoreBranch Create(Guid storeId, BranchInput input, int sortOrder)
    {
        var name = Store.Clean(input.Name);
        var address = Store.Clean(input.Address);
        var phone = Store.Clean(input.Phone);
        var hours = Store.Clean(input.OpeningHours);
        if (name is null or { Length: > MaxNameLength }
            || address is null or { Length: > MaxAddressLength }
            || phone is { Length: > MaxPhoneLength }
            || hours is { Length: > MaxOpeningHoursLength }
            || input.CityId == Guid.Empty)
        {
            throw new DomainException(
                "store_branch.invalid",
                $"A branch needs a name (up to {MaxNameLength}), a town and an address (up to {MaxAddressLength}); phone is up to {MaxPhoneLength} and opening hours up to {MaxOpeningHoursLength} characters.");
        }

        return new StoreBranch
        {
            StoreId = storeId,
            Name = name,
            CityId = input.CityId,
            DistrictId = input.DistrictId,
            Address = address,
            Phone = phone,
            OpeningHours = hours,
            SortOrder = sortOrder,
        };
    }
}
