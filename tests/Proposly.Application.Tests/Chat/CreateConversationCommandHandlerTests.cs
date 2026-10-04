using NSubstitute;
using Proposly.Application.Abstractions;
using Proposly.Application.Chat.Commands.CreateConversation;
using Proposly.Domain.Chat.Entities;
using Proposly.Domain.Chat.Enums;
using Proposly.Domain.Chat.Repositories;
using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.CompanyManagement.Repositories;

namespace Proposly.Application.Tests.Chat;

public sealed class CreateConversationCommandHandlerTests
{
    private readonly IConversationRepository _conversations = Substitute.For<IConversationRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly CreateConversationCommandHandler _sut;

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();

    public CreateConversationCommandHandlerTests()
    {
        _currentUser.CompanyId.Returns(_companyId);
        _currentUser.UserId.Returns(_userId);
        _users.GetByIdAsync(Arg.Any<Guid>())
            .Returns(User.Create(_companyId, "x@test.com", "hash", "Some", "User"));
        _sut = new CreateConversationCommandHandler(_conversations, _users, _currentUser);
    }

    [Fact]
    public async Task HandleAsync_Direct_CreatesWithBothParticipants()
    {
        _conversations.GetDirectBetweenAsync(_userId, _otherUserId).Returns((Conversation?)null);
        Conversation? saved = null;
        await _conversations.AddAsync(Arg.Do<Conversation>(c => saved = c));

        var id = await _sut.HandleAsync(new CreateConversationCommand("Direct", [_otherUserId], null));

        Assert.NotNull(saved);
        Assert.Equal(saved!.Id, id);
        Assert.Equal(ConversationKind.Direct, saved.Kind);
        Assert.True(saved.IsParticipant(_userId));
        Assert.True(saved.IsParticipant(_otherUserId));
    }

    [Fact]
    public async Task HandleAsync_DirectThatAlreadyExists_ReturnsExistingId()
    {
        var existing = Conversation.CreateDirect(_companyId, _userId, _otherUserId);
        _conversations.GetDirectBetweenAsync(_userId, _otherUserId).Returns(existing);

        var id = await _sut.HandleAsync(new CreateConversationCommand("Direct", [_otherUserId], null));

        Assert.Equal(existing.Id, id);
        await _conversations.DidNotReceive().AddAsync(Arg.Any<Conversation>());
    }

    [Fact]
    public async Task HandleAsync_Group_IncludesCreatorAsParticipant()
    {
        Conversation? saved = null;
        await _conversations.AddAsync(Arg.Do<Conversation>(c => saved = c));
        var third = Guid.NewGuid();

        await _sut.HandleAsync(new CreateConversationCommand("Group", [_otherUserId, third], "Offer review"));

        Assert.NotNull(saved);
        Assert.Equal(ConversationKind.Group, saved!.Kind);
        Assert.True(saved.IsParticipant(_userId));
        Assert.Equal(3, saved.Participants.Count);
    }

    [Fact]
    public async Task HandleAsync_UnknownParticipant_Throws()
    {
        _users.GetByIdAsync(Arg.Any<Guid>()).Returns((User?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandleAsync(new CreateConversationCommand("Direct", [_otherUserId], null)));
    }

    [Fact]
    public async Task HandleAsync_OnlySelfAsParticipant_Throws()
        => await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandleAsync(new CreateConversationCommand("Direct", [_userId], null)));
}
