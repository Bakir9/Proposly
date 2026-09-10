namespace Proposly.API.Authorization;

/// <summary>
/// Central registry of all authorization policy names.
/// Use these constants on [Authorize(Policy = ...)] attributes — never hardcode strings in controllers.
/// </summary>
public static class Policies
{
    /// <summary>Company owner only. Use for destructive or billing-related actions.</summary>
    public const string OwnerOnly = nameof(OwnerOnly);

    /// <summary>Owner or Admin. Use for managing team members.</summary>
    public const string ManageUsers = nameof(ManageUsers);

    /// <summary>Owner or Admin. Use for creating/sending/accepting offers.</summary>
    public const string ManageOffers = nameof(ManageOffers);

    /// <summary>Owner or Admin. Use for creating and updating clients.</summary>
    public const string ManageClients = nameof(ManageClients);

    /// <summary>Owner or Admin. Use for creating and managing projects.</summary>
    public const string ManageProjects = nameof(ManageProjects);

    /// <summary>SuperAdmin only. Use for cross-tenant administration (plan management, company listing).</summary>
    public const string SuperAdminOnly = nameof(SuperAdminOnly);

    /// <summary>Any employee (Owner, Admin, Member). Use for recording own working time and requesting own absence.</summary>
    public const string RecordOwnWorkTime = nameof(RecordOwnWorkTime);

    /// <summary>Owner or Admin. Use for approving timesheets and absence requests.</summary>
    public const string ApproveWorkTime = nameof(ApproveWorkTime);

    /// <summary>Owner or Admin. Use for employment terms, the holiday calendar, and the working time policy.</summary>
    public const string ManageWorkTimeSettings = nameof(ManageWorkTimeSettings);

    /// <summary>Owner or Admin. Use for company-wide working time, compliance, and report views.</summary>
    public const string ViewAllWorkTime = nameof(ViewAllWorkTime);
}
