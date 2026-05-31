using Proposly.Application.Abstractions;

namespace Proposly.Application.Auth.Commands.Register;

public record RegisterCommand(
    string CompanyName,
    string FirstName,
    string LastName,
    string Email,
    string Password) : ICommand;
