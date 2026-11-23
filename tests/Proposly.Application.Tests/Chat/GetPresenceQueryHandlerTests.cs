using NSubstitute;
using Proposly.Application.Abstractions;
using Proposly.Application.Chat.Queries.GetPresence;
using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.CompanyManagement.Enums;
using Proposly.Domain.CompanyManagement.Repositories;

namespace Proposly.Application.Tests.Chat;

public sealed class GetPresenceQueryHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPresenceTracker _tracker = Substitute.For<IPresenceTracker>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly GetPresenceQueryHandler _sut;

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _meId = Guid.NewGuid();

    public GetPresenceQueryHandlerTests()
    {
        _currentUser.UserId.Returns(_meId);
        _sut = new GetPresenceQueryHandler(_users, _tracker, _currentUser);
    }

    private User Colleague(string first, DateTime? seenAgo = null)
    {
        var u = User.Create(_companyId, $"{first.ToLowerInvariant()}@test.com", "hash", first, "Tester");
        if (seenAgo is not null) u.RecordSeen(seenAgo.Value);
        return u;
    }

    [Fact]
    public async Task Classifies_online_away_and_offline_from_last_seen()
    {
        var now = DateTime.UtcNow;
        _users.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(
        [
            Colleague("Online", now.AddSeconds(-30)),
            Colleague("Away", now.AddMinutes(-5)),
            Colleague("Gone", now.AddHours(-3)),
            Colleague("Never"),
        ]);

        var result = await _sut.HandleAsync(new GetPresenceQuery());

        Assert.Equal("online", result.Single(r => r.Name.StartsWith("Online")).Presence);
        Assert.Equal("away", result.Single(r => r.Name.StartsWith("Away")).Presence);
        Assert.Equal("offline", result.Single(r => r.Name.StartsWith("Gone")).Presence);
        var never = result.Single(r => r.Name.StartsWith("Never"));
        Assert.Equal("offline", never.Presence);
        Assert.Null(never.LastSeenAt);
    }

    [Fact]
    public async Task Excludes_self_profile_only_workers_and_super_admins()
    {
        var me = User.Create(_companyId, "me@test.com", "hash", "Me", "Myself");
        _currentUser.UserId.Returns(me.Id);

        _users.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(
        [
            me,
            User.CreateProfileOnly(_companyId, "Profile", "Only"),
            User.CreateSuperAdmin("root@proposly.io", "hash", "Super", "Admin"),
            Colleague("Visible"),
        ]);

        var result = await _sut.HandleAsync(new GetPresenceQuery());

        var row = Assert.Single(result);
        Assert.StartsWith("Visible", row.Name);
        Assert.Equal(nameof(UserRole.Member), row.Role);
    }

    [Fact]
    public async Task Live_socket_is_online_even_with_stale_heartbeat()
    {
        var colleague = Colleague("Lukas", DateTime.UtcNow.AddHours(-3)); // heartbeat long stale
        _users.GetActiveAsync(Arg.Any<CancellationToken>()).Returns([colleague]);
        _tracker.IsOnline(colleague.Id).Returns(true);

        var result = await _sut.HandleAsync(new GetPresenceQuery());

        Assert.Equal("online", Assert.Single(result).Presence);
    }

    [Fact]
    public async Task Without_live_socket_heartbeat_decides_away_or_offline()
    {
        var away = Colleague("Away", DateTime.UtcNow.AddMinutes(-5));
        var offline = Colleague("Gone", DateTime.UtcNow.AddHours(-1));
        _users.GetActiveAsync(Arg.Any<CancellationToken>()).Returns([away, offline]);
        _tracker.IsOnline(Arg.Any<Guid>()).Returns(false);

        var result = await _sut.HandleAsync(new GetPresenceQuery());

        Assert.Equal("away", result.Single(p => p.UserId == away.Id).Presence);
        Assert.Equal("offline", result.Single(p => p.UserId == offline.Id).Presence);
    }
}
