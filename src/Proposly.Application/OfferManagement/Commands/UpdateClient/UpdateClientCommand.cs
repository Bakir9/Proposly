using Proposly.Application.Abstractions;

namespace Proposly.Application.OfferManagement.Commands.UpdateClient;

public record UpdateClientCommand(
    Guid ClientId,
    string Name,
    string? ContactPerson,
    string? Email,
    string? Phone,
    string? Street,
    string? City,
    string? PostalCode,
    string? Country) : ICommand;
