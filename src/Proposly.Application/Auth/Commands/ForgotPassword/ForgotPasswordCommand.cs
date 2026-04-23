using Proposly.Application.Abstractions;

namespace Proposly.Application.Auth.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : ICommand;
