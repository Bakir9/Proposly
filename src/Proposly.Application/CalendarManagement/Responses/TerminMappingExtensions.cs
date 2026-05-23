using Proposly.Domain.CalendarManagement.Entities;
using Proposly.Domain.CalendarManagement.Enums;

namespace Proposly.Application.CalendarManagement.Responses;

internal static class TerminMappingExtensions
{
    internal static TerminSummaryResponse ToSummary(this Termin t, Guid currentUserId)
    {
        var isOrganizer = t.OrganizerId == currentUserId;
        var myInv = t.Invitations.FirstOrDefault(i => i.InviteeId == currentUserId);
        return new TerminSummaryResponse(
            t.Id, t.Title, t.Description, t.Start, t.End, t.Location,
            t.Status.ToString(),
            t.OrganizerId, t.OrganizerName,
            isOrganizer ? "Organizer" : "Invitee",
            myInv?.Status.ToString(),
            t.Invitations.Count,
            t.Invitations.Count(i => i.Status == InvitationStatus.Accepted));
    }

    internal static TerminDetailResponse ToDetail(this Termin t, Guid currentUserId)
    {
        var isOrganizer = t.OrganizerId == currentUserId;
        var myInv = t.Invitations.FirstOrDefault(i => i.InviteeId == currentUserId);
        var invitations = t.Invitations
            .Select(i => new InvitationResponse(i.Id, i.InviteeId, i.InviteeName, i.Status.ToString(),
                i.RespondedAt, i.ProposedStart, i.ProposedEnd, i.ProposedMessage))
            .ToList();
        return new TerminDetailResponse(
            t.Id, t.Title, t.Description, t.Start, t.End, t.Location,
            t.Status.ToString(),
            t.OrganizerId, t.OrganizerName,
            isOrganizer ? "Organizer" : "Invitee",
            myInv?.Status.ToString(),
            invitations);
    }
}
