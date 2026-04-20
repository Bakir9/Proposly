using Proposly.Application.Abstractions;

namespace Proposly.Application.UserManagement.Commands.ToggleUserStatus;

public record ToggleUserStatusCommand(Guid UserId, bool Disable) : ICommand;
