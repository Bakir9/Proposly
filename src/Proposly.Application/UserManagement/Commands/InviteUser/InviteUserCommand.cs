using Proposly.Application.Abstractions;
using Proposly.Application.UserManagement.Responses;

namespace Proposly.Application.UserManagement.Commands.InviteUser;

public record InviteUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string Role) : ICommand<UserDetailResponse>;
