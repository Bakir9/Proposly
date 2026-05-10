using Proposly.Domain.OfferManagement.Enums;

namespace Proposly.Application.OfferManagement.Responses;

public record ClientSummaryResponse(
    Guid Id,
    string Name,
    string? ContactPerson,
    string? Email,
    string? Phone,
    string? Website,
    string? Street,
    string? City,
    string? PostalCode,
    string? Country,
    string? Currency,
    string? VatNumber,
    ClientStatus Status);

public record ClientDetailResponse(
    Guid Id,
    string Name,
    string? ContactPerson,
    string? Email,
    string? Phone,
    string? Website,
    string? Street,
    string? City,
    string? PostalCode,
    string? Country,
    string? Currency,
    string? VatNumber,
    DateTime CreatedAt,
    ClientStatus Status,
    IReadOnlyList<ClientNoteResponse> Notes);

public record ClientNoteResponse(
    Guid Id,
    string Content,
    string AuthorName,
    DateTime CreatedAt);
