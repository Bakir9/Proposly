using Proposly.Domain.CompanyManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Services;

/// <summary>
/// Timesheets store only a UserId, so approver views resolve display names in one pass rather
/// than one lookup per row.
/// </summary>
internal static class UserNameLookup
{
    public static async Task<Dictionary<Guid, string>> ResolveNamesAsync(
        this IUserRepository users, CancellationToken ct = default)
    {
        // GetAllAsync rather than GetActiveAsync: a disabled or departed employee still has
        // months on record that an approver must be able to read.
        var all = await users.GetAllAsync(ct);
        return all.ToDictionary(u => u.Id, u => u.FullName);
    }
}
