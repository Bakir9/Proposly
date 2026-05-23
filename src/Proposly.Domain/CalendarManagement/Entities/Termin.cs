using Proposly.Domain.CalendarManagement.Enums;
using Proposly.Domain.CalendarManagement.Events;
using Proposly.Shared.Interfaces;
using Proposly.Shared.Primitives;

namespace Proposly.Domain.CalendarManagement.Entities;

public sealed class Termin : AggregateRoot<Guid>, ITenantEntity, IAuditableEntity
{
    private readonly List<TerminInvitation> _invitations = [];

    private Termin() { }

    private Termin(
        Guid id, Guid companyId, string title, string? description,
        DateTime start, DateTime end, string? location,
        Guid organizerId, string organizerName) : base(id)
    {
        CompanyId = companyId;
        Title = title;
        Description = description;
        Start = start;
        End = end;
        Location = location;
        OrganizerId = organizerId;
        OrganizerName = organizerName;
        Status = TerminStatus.Scheduled;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Termin Create(
        Guid companyId, string title, string? description,
        DateTime start, DateTime end, string? location,
        Guid organizerId, string organizerName)
        => new(Guid.NewGuid(), companyId, title, description, start, end, location, organizerId, organizerName);

    public Guid CompanyId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime Start { get; private set; }
    public DateTime End { get; private set; }
    public string? Location { get; private set; }
    public Guid OrganizerId { get; private set; }
    public string OrganizerName { get; private set; } = string.Empty;
    public TerminStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyCollection<TerminInvitation> Invitations => _invitations.AsReadOnly();

    public void Update(string title, string? description, string? location)
    {
        EnsureScheduled();
        Title = title;
        Description = description;
        Location = location;
        Touch();
    }

    public void Reschedule(DateTime newStart, DateTime newEnd)
    {
        EnsureScheduled();
        if (newEnd <= newStart)
            throw new InvalidOperationException("End time must be after start time.");

        Start = newStart;
        End = newEnd;

        var activeInvitees = _invitations
            .Where(i => i.Status != InvitationStatus.Declined)
            .Select(i => (i.InviteeId, i.InviteeName))
            .ToList();

        foreach (var inv in _invitations.Where(i => i.Status != InvitationStatus.Declined))
            inv.ResetToPending();

        Touch();
        RaiseDomainEvent(new TerminRescheduledDomainEvent(Id, Title, newStart, newEnd, OrganizerId, CompanyId, activeInvitees));
    }

    public void Cancel()
    {
        EnsureScheduled();
        Status = TerminStatus.Cancelled;
        var invitees = _invitations.Select(i => (i.InviteeId, i.InviteeName)).ToList();
        Touch();
        RaiseDomainEvent(new TerminCancelledDomainEvent(Id, Title, OrganizerId, CompanyId, invitees));
    }

    public TerminInvitation AddInvitee(Guid userId, string userName)
    {
        EnsureScheduled();
        if (_invitations.Any(i => i.InviteeId == userId))
            throw new InvalidOperationException($"User {userId} is already invited.");
        if (userId == OrganizerId)
            throw new InvalidOperationException("The organizer cannot be added as an invitee.");

        var inv = TerminInvitation.Create(Id, userId, userName);
        _invitations.Add(inv);
        Touch();
        return inv;
    }

    public void RemoveInvitee(Guid userId)
    {
        var inv = _invitations.FirstOrDefault(i => i.InviteeId == userId)
            ?? throw new InvalidOperationException("Invitation not found.");
        _invitations.Remove(inv);
        Touch();
    }

    public void AcceptInvitation(Guid inviteeId)
    {
        EnsureScheduled();
        var inv = GetInvitation(inviteeId);
        if (inv.Status == InvitationStatus.Declined)
            throw new InvalidOperationException("Cannot accept a declined invitation.");
        inv.Accept();
        Touch();
        RaiseDomainEvent(new InvitationRespondedDomainEvent(Id, Title, inviteeId, inv.InviteeName, InvitationStatus.Accepted, OrganizerId, CompanyId));
    }

    public void DeclineInvitation(Guid inviteeId)
    {
        EnsureScheduled();
        var inv = GetInvitation(inviteeId);
        inv.Decline();
        Touch();
        RaiseDomainEvent(new InvitationRespondedDomainEvent(Id, Title, inviteeId, inv.InviteeName, InvitationStatus.Declined, OrganizerId, CompanyId));
    }

    public void ProposeReschedule(Guid inviteeId, DateTime proposedStart, DateTime proposedEnd, string? message)
    {
        EnsureScheduled();
        if (proposedEnd <= proposedStart)
            throw new InvalidOperationException("Proposed end must be after proposed start.");
        var inv = GetInvitation(inviteeId);
        if (inv.Status != InvitationStatus.Accepted)
            throw new InvalidOperationException("Only accepted invitees can propose a reschedule.");
        inv.ProposeReschedule(proposedStart, proposedEnd, message);
        Touch();
        RaiseDomainEvent(new RescheduleProposedDomainEvent(Id, Title, inviteeId, inv.InviteeName, proposedStart, proposedEnd, OrganizerId, CompanyId));
    }

    public void AcceptRescheduleProposal(Guid inviteeId)
    {
        var inv = GetInvitation(inviteeId);
        if (inv.Status != InvitationStatus.RescheduleProposed)
            throw new InvalidOperationException("No pending reschedule proposal from this invitee.");

        var newStart = inv.ProposedStart!.Value;
        var newEnd = inv.ProposedEnd!.Value;
        Reschedule(newStart, newEnd);
    }

    public void DeclineRescheduleProposal(Guid inviteeId)
    {
        var inv = GetInvitation(inviteeId);
        if (inv.Status != InvitationStatus.RescheduleProposed)
            throw new InvalidOperationException("No pending reschedule proposal from this invitee.");
        inv.RevertToAccepted();
        Touch();
    }

    public void PublishScheduled()
    {
        var invitees = _invitations.Select(i => (i.InviteeId, i.InviteeName)).ToList();
        if (invitees.Count > 0)
            RaiseDomainEvent(new TerminScheduledDomainEvent(Id, Title, Start, OrganizerId, OrganizerName, CompanyId, invitees));
    }

    // ---- helpers ----

    private TerminInvitation GetInvitation(Guid inviteeId)
        => _invitations.FirstOrDefault(i => i.InviteeId == inviteeId)
           ?? throw new InvalidOperationException("Invitation not found.");

    private void EnsureScheduled()
    {
        if (Status != TerminStatus.Scheduled)
            throw new InvalidOperationException("This meeting has been cancelled.");
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
