using Proposly.Application.Abstractions;
using Proposly.Domain.OfferManagement.Enums;

namespace Proposly.Application.OfferManagement.Commands.UpdateClient;

public record UpdateClientCommand(
    Guid ClientId,
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
    ClientStatus Status = ClientStatus.Active) : ICommand;
