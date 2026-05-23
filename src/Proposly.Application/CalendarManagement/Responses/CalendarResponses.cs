namespace Proposly.Application.CalendarManagement.Responses;

public record TerminSummaryResponse(
    Guid Id,
    string Title,
    string? Description,
    DateTime Start,
    DateTime End,
    string? Location,
    string Status,
    Guid OrganizerId,
    string OrganizerName,
    string MyRole,
    string? MyInvitationStatus,
    int InvitationCount,
    int AcceptedCount);

public record TerminDetailResponse(
    Guid Id,
    string Title,
    string? Description,
    DateTime Start,
    DateTime End,
    string? Location,
    string Status,
    Guid OrganizerId,
    string OrganizerName,
    string MyRole,
    string? MyInvitationStatus,
    IReadOnlyList<InvitationResponse> Invitations);

public record InvitationResponse(
    Guid Id,
    Guid InviteeId,
    string InviteeName,
    string Status,
    DateTime? RespondedAt,
    DateTime? ProposedStart,
    DateTime? ProposedEnd,
    string? ProposedMessage);

public record UserAvailabilityResponse(
    Guid UserId,
    IReadOnlyList<TerminSummaryResponse> Termins);
