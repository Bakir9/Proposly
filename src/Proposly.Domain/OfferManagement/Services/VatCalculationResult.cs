using Proposly.Domain.OfferManagement.Enums;

namespace Proposly.Domain.OfferManagement.Services;

public sealed class VatCalculationResult
{
    public decimal Rate { get; init; }
    public VatType Type { get; init; }
    public string Label { get; init; } = string.Empty;
    public string? Note { get; init; }
    public bool IsExempt { get; init; }
}
