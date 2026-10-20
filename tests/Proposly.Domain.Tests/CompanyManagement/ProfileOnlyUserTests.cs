using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.CompanyManagement.Enums;

namespace Proposly.Domain.Tests.CompanyManagement;

public class ProfileOnlyUserTests
{
    [Fact]
    public void CreateProfileOnly_HasNoLoginPath()
    {
        var user = User.CreateProfileOnly(Guid.NewGuid(), "Mujo", "Mujic");

        Assert.True(user.IsProfileOnly);
        Assert.False(user.IsPendingInvite);
        Assert.False(user.IsEmailVerified);
        Assert.Equal(UserRole.Member, user.Role);
        Assert.EndsWith("@profile.invalid", user.Email);
    }

    [Fact]
    public void CreateProfileOnly_GeneratesUniquePlaceholderEmails()
    {
        var a = User.CreateProfileOnly(Guid.NewGuid(), "A", "A");
        var b = User.CreateProfileOnly(Guid.NewGuid(), "B", "B");
        Assert.NotEqual(a.Email, b.Email);
    }

    [Fact]
    public void GrantLoginAccess_UpgradesToInvitedAccount()
    {
        var user = User.CreateProfileOnly(Guid.NewGuid(), "Mujo", "Mujic");

        user.GrantLoginAccess("mujo@test.at", UserRole.Member, "token-123", DateTime.UtcNow.AddDays(7));

        Assert.False(user.IsProfileOnly);
        Assert.True(user.IsPendingInvite);
        Assert.True(user.IsEmailVerified);
        Assert.Equal("mujo@test.at", user.Email);
    }

    [Fact]
    public void GrantLoginAccess_OnRegularAccount_Throws()
    {
        var user = User.Create(Guid.NewGuid(), "a@b.c", "hash", "A", "B");
        Assert.Throws<InvalidOperationException>(
            () => user.GrantLoginAccess("x@y.z", UserRole.Member, "t", DateTime.UtcNow));
    }

    [Fact]
    public void GrantLoginAccess_AsOwner_Throws()
    {
        var user = User.CreateProfileOnly(Guid.NewGuid(), "Mujo", "Mujic");
        Assert.Throws<InvalidOperationException>(
            () => user.GrantLoginAccess("x@y.z", UserRole.Owner, "t", DateTime.UtcNow));
    }
}
