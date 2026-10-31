using Proposly.Application.Abstractions;
using Proposly.Application.Chat.Responses;
using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.CompanyManagement.Enums;
using Proposly.Domain.CompanyManagement.Repositories;

namespace Proposly.Application.Chat.Queries.GetPresence;

/// <summary>
/// Colleagues of the current company (login-capable, enabled) with presence derived from the
/// LastSeenAt heartbeat: online &lt; 2 min, away &lt; 15 min, else offline. The current user is
/// excluded — the dock lists people to talk to.
/// </summary>
public sealed class GetPresenceQueryHandler(
    IUserRepository users,
    ICurrentUserService currentUser) : IQueryHandler<GetPresenceQuery, IReadOnlyList<PresenceResponse>>
{
    private static readonly TimeSpan OnlineWindow = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan AwayWindow = TimeSpan.FromMinutes(15);

    public async Task<IReadOnlyList<PresenceResponse>> HandleAsync(GetPresenceQuery query, CancellationToken ct = default)
    {
        var active = await users.GetActiveAsync(ct); // tenant-filtered, enabled users
        var now = DateTime.UtcNow;

        return active
            .Where(u => u.Id != currentUser.UserId && !u.IsProfileOnly && u.Role != UserRole.SuperAdmin)
            .Select(u => new PresenceResponse(u.Id, u.FullName, u.Role.ToString(), PresenceOf(u, now), u.LastSeenAt))
            .ToList();
    }

    private static string PresenceOf(User user, DateTime now)
    {
        if (user.LastSeenAt is null) return "offline";
        var age = now - user.LastSeenAt.Value;
        if (age < OnlineWindow) return "online";
        if (age < AwayWindow) return "away";
        return "offline";
    }
}
