using Proposly.Domain.CalendarManagement.Enums;
using Proposly.Shared.Primitives;

namespace Proposly.Domain.CalendarManagement.Entities;

public sealed class TerminInvitation : Entity<Guid>
{
    private TerminInvitation() { }

    private TerminInvitation(Guid id, Guid terminId, Guid inviteeId, string inviteeName) : base(id)
    {
        TerminId = terminId;
        InviteeId = inviteeId;
        InviteeName = inviteeName;
        Status = InvitationStatus.Pending;
    }

    public static TerminInvitation Create(Guid terminId, Guid inviteeId, string inviteeName)
        => new(Guid.NewGuid(), terminId, inviteeId, inviteeName);

    public Guid TerminId { get; private set; }
    public Guid InviteeId { get; private set; }
    public string InviteeName { get; private set; } = string.Empty;
    public InvitationStatus Status { get; private set; }
    public DateTime? RespondedAt { get; private set; }
    public DateTime? ProposedStart { get; private set; }
    public DateTime? ProposedEnd { get; private set; }
    public string? ProposedMessage { get; private set; }

    internal void Accept()
    {
        Status = InvitationStatus.Accepted;
        RespondedAt = DateTime.UtcNow;
        ProposedStart = null;
        ProposedEnd = null;
        ProposedMessage = null;
    }

    internal void Decline()
    {
        Status = InvitationStatus.Declined;
        RespondedAt = DateTime.UtcNow;
        ProposedStart = null;
        ProposedEnd = null;
        ProposedMessage = null;
    }

    internal void ProposeReschedule(DateTime proposedStart, DateTime proposedEnd, string? message)
    {
        Status = InvitationStatus.RescheduleProposed;
        ProposedStart = proposedStart;
        ProposedEnd = proposedEnd;
        ProposedMessage = message;
        RespondedAt = DateTime.UtcNow;
    }

    internal void ResetToPending()
    {
        Status = InvitationStatus.Pending;
        RespondedAt = null;
        ProposedStart = null;
        ProposedEnd = null;
        ProposedMessage = null;
    }

    internal void RevertToAccepted()
    {
        Status = InvitationStatus.Accepted;
        ProposedStart = null;
        ProposedEnd = null;
        ProposedMessage = null;
    }
}
