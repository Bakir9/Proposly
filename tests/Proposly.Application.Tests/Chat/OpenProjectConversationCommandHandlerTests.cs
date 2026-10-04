using NSubstitute;
using Proposly.Application.Abstractions;
using Proposly.Application.Chat.Commands.OpenProjectConversation;
using Proposly.Domain.Chat.Entities;
using Proposly.Domain.Chat.Repositories;
using Proposly.Domain.ProjectManagement.Entities;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Shared.ValueObjects;

namespace Proposly.Application.Tests.Chat;

public sealed class OpenProjectConversationCommandHandlerTests
{
    private readonly IConversationRepository _conversations = Substitute.For<IConversationRepository>();
    private readonly IProjectRepository _projects = Substitute.For<IProjectRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly OpenProjectConversationCommandHandler _sut;

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public OpenProjectConversationCommandHandlerTests()
    {
        _currentUser.CompanyId.Returns(_companyId);
        _currentUser.UserId.Returns(_userId);
        _sut = new OpenProjectConversationCommandHandler(_conversations, _projects, _currentUser);
    }

    private Project MakeProject() =>
        Project.Create(_companyId, "Kitchen renovation", null, Guid.NewGuid(), "Client", Money.Zero("EUR"),
            DateOnly.FromDateTime(DateTime.UtcNow), null);

    [Fact]
    public async Task HandleAsync_MemberWithoutChannel_CreatesChannelWithMembers()
    {
        var project = MakeProject();
        project.AddMember(_userId, "Anna Tester", "Dev", Money.Zero("EUR"));
        _projects.GetByIdForWriteAsync(project.Id).Returns(project);
        _conversations.GetByProjectIdAsync(project.Id).Returns((Conversation?)null);
        Conversation? saved = null;
        await _conversations.AddAsync(Arg.Do<Conversation>(c => saved = c));

        var id = await _sut.HandleAsync(new OpenProjectConversationCommand(project.Id));

        Assert.NotNull(saved);
        Assert.Equal(saved!.Id, id);
        Assert.Equal(project.Id, saved.ProjectId);
        Assert.Equal("Kitchen renovation", saved.Title);
        Assert.True(saved.IsParticipant(_userId));
    }

    [Fact]
    public async Task HandleAsync_AdminWhoIsNotMember_JoinsExistingChannel()
    {
        _currentUser.CanViewAllEmployees.Returns(true);
        var project = MakeProject();
        var memberUserId = Guid.NewGuid();
        project.AddMember(memberUserId, "Lukas", "Site manager", Money.Zero("EUR"));
        var channel = Conversation.CreateProjectChannel(_companyId, project.Id, project.Name);
        _projects.GetByIdForWriteAsync(project.Id).Returns(project);
        _conversations.GetByProjectIdAsync(project.Id).Returns(channel);

        var id = await _sut.HandleAsync(new OpenProjectConversationCommand(project.Id));

        Assert.Equal(channel.Id, id);
        Assert.True(channel.IsParticipant(_userId));
        Assert.True(channel.IsParticipant(memberUserId));
        await _conversations.DidNotReceive().AddAsync(Arg.Any<Conversation>());
    }

    [Fact]
    public async Task HandleAsync_PlainUserWhoIsNotMember_Throws()
    {
        _currentUser.CanViewAllEmployees.Returns(false);
        var project = MakeProject();
        _projects.GetByIdForWriteAsync(project.Id).Returns(project);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandleAsync(new OpenProjectConversationCommand(project.Id)));
    }

    [Fact]
    public async Task HandleAsync_UnknownProject_Throws()
    {
        _projects.GetByIdForWriteAsync(Arg.Any<Guid>()).Returns((Project?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandleAsync(new OpenProjectConversationCommand(Guid.NewGuid())));
    }
}
