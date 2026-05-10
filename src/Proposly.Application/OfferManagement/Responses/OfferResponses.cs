using Proposly.Domain.OfferManagement.Enums;

namespace Proposly.Application.OfferManagement.Responses;

public record OfferSummaryResponse(
    Guid Id,
    Guid ClientId,
    string ClientName,
    string Title,
    OfferStatus Status,
    decimal Subtotal,
    decimal? DiscountPercent,
    decimal DiscountAmount,
    decimal Total,
    string Currency,
    DateOnly? ValidUntil,
    DateTime CreatedAt);

public record OfferDetailResponse(
    Guid Id,
    Guid ClientId,
    string ClientName,
    string Title,
    string? Notes,
    OfferStatus Status,
    decimal Subtotal,
    decimal? DiscountPercent,
    decimal DiscountAmount,
    decimal Total,
    string Currency,
    DateOnly? ValidUntil,
    DateTime? SentAt,
    DateTime CreatedAt,
    IReadOnlyList<OfferItemResponse> Items);

public record OfferItemResponse(
    Guid Id,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    string Currency);
