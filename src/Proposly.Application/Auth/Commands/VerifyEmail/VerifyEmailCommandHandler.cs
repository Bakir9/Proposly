using Proposly.Application.Abstractions;
using Proposly.Application.Auth.Responses;
using Proposly.Domain.CompanyManagement.Repositories;

namespace Proposly.Application.Auth.Commands.VerifyEmail;

public sealed class VerifyEmailCommandHandler : ICommandHandler<VerifyEmailCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;

    public VerifyEmailCommandHandler(IUserRepository userRepository, IJwtService jwtService)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
    }

    public async Task<AuthResponse> HandleAsync(VerifyEmailCommand command, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByEmailVerificationTokenAsync(command.Token, ct)
            ?? throw new InvalidOperationException("Invalid or expired verification link.");

        if (user.EmailVerificationTokenExpiry < DateTime.UtcNow)
            throw new InvalidOperationException("This verification link has expired. Please register again.");

        user.MarkEmailVerified();
        await _userRepository.UpdateAsync(user, ct);

        var token = _jwtService.GenerateToken(user);
        return new AuthResponse(token, user.Id, user.CompanyId, user.FullName, user.Email, user.Role.ToString());
    }
}
