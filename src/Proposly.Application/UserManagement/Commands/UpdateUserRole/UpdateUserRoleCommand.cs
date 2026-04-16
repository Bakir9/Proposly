using Proposly.Application.Abstractions;

namespace Proposly.Application.UserManagement.Commands.UpdateUserRole;

public record UpdateUserRoleCommand(Guid UserId, string Role) : ICommand;
