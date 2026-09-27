namespace Proposly.Application.Abstractions;

public interface ICurrentUserService
{
    Guid CompanyId { get; }
    Guid UserId { get; }
    string Role { get; }

    /// <summary>
    /// True when the current user may see every employee's user-owned records in their company
    /// (Owner, Admin, SuperAdmin). Read by the IUserOwnedEntity query filter, so it MUST NOT
    /// throw — outside an HTTP request it returns false, the most restrictive answer.
    /// </summary>
    bool CanViewAllEmployees { get; }
}
