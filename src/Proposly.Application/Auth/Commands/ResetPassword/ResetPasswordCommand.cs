using Proposly.Application.Abstractions;

namespace Proposly.Application.Auth.Commands.ResetPassword;

public sealed record ResetPasswordCommand(string Token, string NewPassword) : ICommand;
