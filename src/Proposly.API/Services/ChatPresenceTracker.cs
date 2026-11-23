using Microsoft.AspNetCore.SignalR;
using Proposly.API.Hubs;
using Proposly.Application.Abstractions;

namespace Proposly.API.Services;

/// <summary>
/// Counts open hub connections per user (one user can hold several tabs) and broadcasts
/// "presence:changed" to the user's company group on the 0→1 and 1→0 transitions.
/// Singleton — one registry per server instance, which is all we run.
/// </summary>
public sealed class ChatPresenceTracker(IHubContext<ChatHub> hub) : IPresenceTracker
{
    private static readonly TimeSpan OfflineGrace = TimeSpan.FromSeconds(10);

    private readonly Dictionary<Guid, int> _connections = [];
    private readonly object _gate = new();

    public bool IsOnline(Guid userId)
    {
        lock (_gate) return _connections.TryGetValue(userId, out var n) && n > 0;
    }

    public async Task HandleConnectedAsync(Guid userId, Guid companyId)
    {
        bool firstConnection;
        lock (_gate)
        {
            _connections.TryGetValue(userId, out var n);
            _connections[userId] = n + 1;
            firstConnection = n == 0;
        }

        if (firstConnection)
            await hub.Clients.Group($"company:{companyId}")
                .SendAsync("presence:changed", userId, "online");
    }

    public void HandleDisconnected(Guid userId, Guid companyId)
    {
        bool lastConnection;
        lock (_gate)
        {
            _connections.TryGetValue(userId, out var n);
            if (n <= 1) { _connections.Remove(userId); lastConnection = n == 1; }
            else { _connections[userId] = n - 1; lastConnection = false; }
        }
        if (!lastConnection) return;

        // Grace period: a page refresh reconnects within seconds — don't flicker offline.
        _ = Task.Run(async () =>
        {
            await Task.Delay(OfflineGrace);
            if (!IsOnline(userId))
                await hub.Clients.Group($"company:{companyId}")
                    .SendAsync("presence:changed", userId, "offline");
        });
    }
}
