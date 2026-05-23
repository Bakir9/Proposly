namespace Proposly.Application.OfferManagement.Responses;

public record VatPreviewResponse(
    decimal Rate,
    string Type,
    string Label,
    string? Note,
    bool IsExempt);
