namespace Proposly.Application.Abstractions;

/// <summary>
/// Who has a live chat connection right now. Implemented over SignalR in the API layer;
/// the presence query treats a live socket as authoritative "online".
/// </summary>
public interface IPresenceTracker
{
    bool IsOnline(Guid userId);
}
