using Proposly.Application.Abstractions;

namespace Proposly.Application.UserManagement.Commands.UpdateProfile;

public record UpdateProfileCommand(string FirstName, string LastName, string Email) : ICommand;
