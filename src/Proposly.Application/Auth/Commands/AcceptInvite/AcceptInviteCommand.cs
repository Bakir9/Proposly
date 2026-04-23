using Proposly.Application.Abstractions;

namespace Proposly.Application.Auth.Commands.AcceptInvite;

public sealed record AcceptInviteCommand(string Token, string NewPassword) : ICommand;
