using Proposly.Application.Abstractions;
using Proposly.Application.Auth.Responses;

namespace Proposly.Application.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : ICommand<AuthResponse>;
