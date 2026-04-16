namespace Proposly.Application.OfferManagement.Responses;

public record ClientSummaryResponse(
    Guid Id,
    string Name,
    string? ContactPerson,
    string? Email,
    string? Phone);

public record ClientDetailResponse(
    Guid Id,
    string Name,
    string? ContactPerson,
    string? Email,
    string? Phone,
    string? Street,
    string? City,
    string? PostalCode,
    string? Country,
    DateTime CreatedAt);
