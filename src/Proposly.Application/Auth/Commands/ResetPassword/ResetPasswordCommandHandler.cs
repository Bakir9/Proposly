using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Shared.Exceptions;

namespace Proposly.Application.Auth.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public ResetPasswordCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task HandleAsync(ResetPasswordCommand command, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByResetTokenAsync(command.Token, ct)
            ?? throw new AppException("auth.invalid_reset_link", "Invalid or expired reset link.");

        if (user.PasswordResetTokenExpiry is null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
            throw new AppException("auth.invalid_reset_link", "Invalid or expired reset link.");

        user.ChangePassword(_passwordHasher.Hash(command.NewPassword));
        user.ClearPasswordResetToken();
        await _userRepository.UpdateAsync(user, ct);
    }
}
