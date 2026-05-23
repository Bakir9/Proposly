using Proposly.Domain.OfferManagement.Entities;
using Proposly.Domain.OfferManagement.Enums;

namespace Proposly.Domain.OfferManagement.Services;

public sealed class VatCalculator
{
    private static readonly IReadOnlyDictionary<string, decimal> EuVatRates =
        new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            { "AT", 20m }, { "BE", 21m }, { "BG", 20m },
            { "CY", 19m }, { "CZ", 21m }, { "DE", 19m },
            { "DK", 25m }, { "EE", 22m }, { "ES", 21m },
            { "FI", 24m }, { "FR", 20m }, { "GR", 24m },
            { "HR", 25m }, { "HU", 27m }, { "IE", 23m },
            { "IT", 22m }, { "LT", 21m }, { "LU", 17m },
            { "LV", 21m }, { "MT", 18m }, { "NL", 21m },
            { "PL", 23m }, { "PT", 23m }, { "RO", 19m },
            { "SE", 25m }, { "SI", 22m }, { "SK", 20m }
        };

    private readonly string? _companyCountry;
    private readonly bool _isVatRegistered;
    private readonly bool _isVatExempt;
    private readonly decimal _defaultVatRate;
    private readonly string? _vatExemptReason;

    public VatCalculator(
        string? companyCountry,
        bool isVatRegistered,
        bool isVatExempt,
        decimal defaultVatRate,
        string? vatExemptReason = null)
    {
        _companyCountry = companyCountry;
        _isVatRegistered = isVatRegistered;
        _isVatExempt = isVatExempt;
        _defaultVatRate = defaultVatRate;
        _vatExemptReason = vatExemptReason;
    }

    public VatCalculationResult Calculate(Client client)
    {
        if (_isVatExempt)
            return new VatCalculationResult
            {
                Rate = 0,
                Type = VatType.Exempt,
                Label = "VAT exempt",
                Note = _vatExemptReason,
                IsExempt = true
            };

        if (!_isVatRegistered)
            return new VatCalculationResult
            {
                Rate = 0,
                Type = VatType.Exempt,
                Label = "No VAT",
                IsExempt = true
            };

        var clientCountry = client.Address?.Country;

        if (string.IsNullOrWhiteSpace(clientCountry) || !IsEuCountry(clientCountry))
            return new VatCalculationResult
            {
                Rate = 0,
                Type = VatType.NonEu,
                Label = "VAT 0% (Export)"
            };

        if (string.Equals(clientCountry, _companyCountry, StringComparison.OrdinalIgnoreCase))
            return new VatCalculationResult
            {
                Rate = _defaultVatRate,
                Type = VatType.Domestic,
                Label = $"VAT {_defaultVatRate}%"
            };

        if (!string.IsNullOrWhiteSpace(client.VatNumber))
            return new VatCalculationResult
            {
                Rate = 0,
                Type = VatType.ReverseCharge,
                Label = "VAT 0% (Reverse Charge)",
                Note = "VAT to be accounted by the recipient pursuant to Article 196 of Council Directive 2006/112/EC"
            };

        if (EuVatRates.TryGetValue(clientCountry, out var clientRate))
            return new VatCalculationResult
            {
                Rate = clientRate,
                Type = VatType.EuConsumer,
                Label = $"VAT {clientRate}% ({clientCountry.ToUpper()})"
            };

        return new VatCalculationResult
        {
            Rate = 0,
            Type = VatType.NonEu,
            Label = "VAT 0% (Export)"
        };
    }

    public static bool IsEuCountry(string countryCode) =>
        EuVatRates.ContainsKey(countryCode);
}
