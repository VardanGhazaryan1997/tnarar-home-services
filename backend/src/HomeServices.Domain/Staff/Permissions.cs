namespace HomeServices.Domain.Staff;

/// <summary>
/// Every Back Office permission. Permissions live in code because each one guards code;
/// roles (data) bundle them. Super Admins have all of them.
/// </summary>
public static class Permissions
{
    public const string DashboardView = "dashboard.view";
    public const string CatalogManage = "catalog.manage";
    public const string PartnersView = "partners.view";
    public const string PartnersApprove = "partners.approve";
    public const string UsersView = "users.view";
    public const string UsersBlock = "users.block";
    public const string StaffView = "staff.view";
    public const string StaffManage = "staff.manage";
    public const string RolesManage = "roles.manage";
    public const string ContentManage = "content.manage";
    public const string TranslationsManage = "translations.manage";
    public const string AuditView = "audit.view";
    public const string RequestsView = "requests.view";
    public const string RequestsManage = "requests.manage";
    public const string OrdersView = "orders.view";
    public const string OrdersManage = "orders.manage";
    public const string PaymentsView = "payments.view";
    public const string PaymentsManage = "payments.manage";
    public const string CommissionsView = "commissions.view";
    public const string CommissionsManage = "commissions.manage";
    public const string SupportManage = "support.manage";
    public const string ReviewsModerate = "reviews.moderate";
    public const string MaterialsPricing = "materials.pricing";
    public const string ReportsView = "reports.view";
    public const string PaymentSettingsManage = "settings.payments";

    /// <summary>Reserved for Super Admins: can't be put in a role.</summary>
    public static readonly IReadOnlySet<string> SuperAdminOnly = new HashSet<string>(StringComparer.Ordinal)
    {
        RolesManage,
        PaymentSettingsManage,
    };

    public static readonly IReadOnlyList<string> All =
    [
        DashboardView, CatalogManage, PartnersView, PartnersApprove, UsersView, UsersBlock,
        StaffView, StaffManage, RolesManage, ContentManage, TranslationsManage, AuditView,
        RequestsView, RequestsManage, OrdersView, OrdersManage, PaymentsView, PaymentsManage,
        CommissionsView, CommissionsManage, SupportManage, ReviewsModerate, MaterialsPricing,
        ReportsView, PaymentSettingsManage,
    ];

    private static readonly HashSet<string> Known = new(All, StringComparer.Ordinal);

    public static bool IsKnown(string permission) => Known.Contains(permission);

    public static bool IsGrantable(string permission) => IsKnown(permission) && !SuperAdminOnly.Contains(permission);
}
