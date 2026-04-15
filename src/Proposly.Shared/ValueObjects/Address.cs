namespace Proposly.Shared.ValueObjects;

public sealed record Address(
    string Street,
    string City,
    string PostalCode,
    string Country);
