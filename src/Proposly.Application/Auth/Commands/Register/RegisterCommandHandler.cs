using Proposly.Application.Abstractions;
using Proposly.Application.Auth.Responses;
using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.CompanyManagement.Enums;
using Proposly.Domain.CompanyManagement.Repositories;

namespace Proposly.Application.Auth.Commands.Register;

public sealed class RegisterCommandHandler : ICommandHandler<RegisterCommand, AuthResponse>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;

    public RegisterCommandHandler(
        ICompanyRepository companyRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtService jwtService)
    {
        _companyRepository = companyRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public async Task<AuthResponse> HandleAsync(RegisterCommand command, CancellationToken ct = default)
    {
        if (await _userRepository.ExistsByEmailAsync(command.Email, ct))
            throw new InvalidOperationException($"Email '{command.Email}' is already registered.");

        var company = Company.Create(command.CompanyName);
        var passwordHash = _passwordHasher.Hash(command.Password);
        var user = User.Create(company.Id, command.Email, passwordHash, command.FirstName, command.LastName, UserRole.Owner);

        await _companyRepository.AddAsync(company, ct);
        await _userRepository.AddAsync(user, ct);

        var token = _jwtService.GenerateToken(user);

        return new AuthResponse(token, user.Id, company.Id, user.FullName, user.Email, user.Role.ToString());
    }
}
