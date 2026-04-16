using Proposly.Application.Abstractions;

namespace Proposly.Application.UserManagement.Commands.RemoveUser;

public record RemoveUserCommand(Guid UserId) : ICommand;
