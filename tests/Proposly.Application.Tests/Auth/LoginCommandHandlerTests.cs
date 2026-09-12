using NSubstitute;
using Proposly.Application.Abstractions;
using Proposly.Application.Auth.Commands.Login;
using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.CompanyManagement.Enums;
using Proposly.Domain.CompanyManagement.Repositories;

namespace Proposly.Application.Tests.Auth;

public sealed class LoginCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtService _jwt = Substitute.For<IJwtService>();
    private readonly LoginCommandHandler _sut;

    public LoginCommandHandlerTests()
    {
        _sut = new LoginCommandHandler(_users, _hasher, _jwt);
    }

    [Fact]
    public async Task HandleAsync_ValidCredentials_ReturnsToken()
    {
        var user = User.Create(Guid.NewGuid(), "alice@test.com", "hash", "Alice", "Smith", UserRole.Member);
        user.MarkEmailVerified();
        _users.GetByEmailAsync("alice@test.com").Returns(user);
        _hasher.Verify("secret", "hash").Returns(true);
        _jwt.GenerateToken(user).Returns("jwt-token");

        var result = await _sut.HandleAsync(new LoginCommand("alice@test.com", "secret"));

        Assert.Equal("jwt-token", result.Token);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal("Alice Smith", result.FullName);
    }

    [Fact]
    public async Task HandleAsync_UnknownEmail_ThrowsUnauthorized()
    {
        _users.GetByEmailAsync("x@x.com").Returns((User?)null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.HandleAsync(new LoginCommand("x@x.com", "pw")));
    }

    [Fact]
    public async Task HandleAsync_WrongPassword_ThrowsUnauthorized()
    {
        var user = User.Create(Guid.NewGuid(), "alice@test.com", "hash", "Alice", "Smith");
        _users.GetByEmailAsync("alice@test.com").Returns(user);
        _hasher.Verify("wrong", "hash").Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.HandleAsync(new LoginCommand("alice@test.com", "wrong")));
    }

    [Fact]
    public async Task HandleAsync_DisabledUser_ThrowsUnauthorized()
    {
        // Verified first, so this actually reaches the disabled check rather than tripping on the
        // unverified-email guard and passing for the wrong reason.
        var user = User.Create(Guid.NewGuid(), "alice@test.com", "hash", "Alice", "Smith");
        user.MarkEmailVerified();
        user.Disable();
        _users.GetByEmailAsync("alice@test.com").Returns(user);
        _hasher.Verify("secret", "hash").Returns(true);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.HandleAsync(new LoginCommand("alice@test.com", "secret")));

        Assert.Contains("disabled", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_UnverifiedEmail_ThrowsUnauthorized()
    {
        var user = User.Create(Guid.NewGuid(), "alice@test.com", "hash", "Alice", "Smith");
        _users.GetByEmailAsync("alice@test.com").Returns(user);
        _hasher.Verify("secret", "hash").Returns(true);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.HandleAsync(new LoginCommand("alice@test.com", "secret")));

        Assert.Contains("verify your email", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidLogin_CallsGenerateToken()
    {
        var user = User.Create(Guid.NewGuid(), "alice@test.com", "hash", "Alice", "Smith");
        user.MarkEmailVerified();
        _users.GetByEmailAsync("alice@test.com").Returns(user);
        _hasher.Verify("secret", "hash").Returns(true);
        _jwt.GenerateToken(user).Returns("token");

        await _sut.HandleAsync(new LoginCommand("alice@test.com", "secret"));

        _jwt.Received(1).GenerateToken(user);
    }
}
