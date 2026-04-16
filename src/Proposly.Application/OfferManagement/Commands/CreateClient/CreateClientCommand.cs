using Proposly.Application.Abstractions;

namespace Proposly.Application.OfferManagement.Commands.CreateClient;

public record CreateClientCommand(
    string Name,
    string? ContactPerson,
    string? Email,
    string? Phone,
    string? Street,
    string? City,
    string? PostalCode,
    string? Country) : ICommand<Guid>;
