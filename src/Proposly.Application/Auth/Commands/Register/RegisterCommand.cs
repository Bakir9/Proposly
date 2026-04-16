using Proposly.Application.Abstractions;
using Proposly.Application.Auth.Responses;

namespace Proposly.Application.Auth.Commands.Register;

public record RegisterCommand(
    string CompanyName,
    string FirstName,
    string LastName,
    string Email,
    string Password) : ICommand<AuthResponse>;
