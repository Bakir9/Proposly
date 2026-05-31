using Proposly.Application.Abstractions;
using Proposly.Application.Auth.Responses;

namespace Proposly.Application.Auth.Commands.VerifyEmail;

public record VerifyEmailCommand(string Token) : ICommand<AuthResponse>;
