using Proposly.Application.Abstractions;
using Proposly.Application.Chat.Responses;
using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.CompanyManagement.Enums;
using Proposly.Domain.CompanyManagement.Repositories;

namespace Proposly.Application.Chat.Queries.GetPresence;

/// <summary>
/// Colleagues of the current company (login-capable, enabled) with presence. A live chat
/// socket (IPresenceTracker) is authoritative "online"; otherwise the LastSeenAt heartbeat
/// decides: online &lt; 2 min, away &lt; 15 min, else offline. The current user is excluded —
/// the dock lists people to talk to.
/// </summary>
public sealed class GetPresenceQueryHandler(
    IUserRepository users,
    IPresenceTracker tracker,
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

    private string PresenceOf(User user, DateTime now)
    {
        if (tracker.IsOnline(user.Id)) return "online";
        if (user.LastSeenAt is null) return "offline";
        var age = now - user.LastSeenAt.Value;
        // The heartbeat still covers the gap right after an API restart, before sockets redial.
        if (age < OnlineWindow) return "online";
        if (age < AwayWindow) return "away";
        return "offline";
    }
}
