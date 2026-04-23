using Proposly.Application.Abstractions;
using Proposly.Application.Auth.Responses;
using Proposly.Domain.CompanyManagement.Repositories;

namespace Proposly.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtService jwtService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public async Task<AuthResponse> HandleAsync(LoginCommand command, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByEmailAsync(command.Email, ct)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!_passwordHasher.Verify(command.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (user.IsPendingInvite)
            throw new UnauthorizedAccessException("Please accept your invite first. Check your email for the activation link.");

        if (user.IsDisabled)
            throw new UnauthorizedAccessException("This account has been disabled. Contact your administrator.");

        var token = _jwtService.GenerateToken(user);

        return new AuthResponse(token, user.Id, user.CompanyId, user.FullName, user.Email, user.Role.ToString());
    }
}
