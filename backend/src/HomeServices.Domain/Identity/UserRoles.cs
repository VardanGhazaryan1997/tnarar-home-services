namespace HomeServices.Domain.Identity;

/// <summary>Portal roles. One account can have several (a plumber can also order as a customer).</summary>
[Flags]
public enum UserRoles
{
    None = 0,
    Customer = 1,
    Partner = 2,
}

public enum UserStatus
{
    Active = 1,
    Blocked = 2,
}
