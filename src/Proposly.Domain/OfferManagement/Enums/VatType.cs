namespace Proposly.Domain.OfferManagement.Enums;

public enum VatType
{
    Domestic,       // same country as company
    ReverseCharge,  // EU B2B with VAT number → 0%
    EuConsumer,     // EU B2C without VAT number
    NonEu,          // outside EU → 0%
    Exempt          // company is VAT exempt or not registered
}
