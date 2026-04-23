using Proposly.Application.Abstractions;

namespace Proposly.Application.UserManagement.Commands.InviteUser;

public record InviteUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string Role) : ICommand;
