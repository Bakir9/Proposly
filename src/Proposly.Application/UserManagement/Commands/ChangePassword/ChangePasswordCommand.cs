using Proposly.Application.Abstractions;

namespace Proposly.Application.UserManagement.Commands.ChangePassword;

public record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommand;
