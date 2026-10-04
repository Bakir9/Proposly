using NSubstitute;
using Proposly.Application.Abstractions;
using Proposly.Application.Chat.Commands.MarkConversationUnread;
using Proposly.Domain.Chat.Entities;
using Proposly.Domain.Chat.Repositories;

namespace Proposly.Application.Tests.Chat;

public sealed class MarkConversationUnreadCommandHandlerTests
{
    private readonly IConversationRepository _conversations = Substitute.For<IConversationRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly MarkConversationUnreadCommandHandler _sut;

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public MarkConversationUnreadCommandHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _sut = new MarkConversationUnreadCommandHandler(_conversations, _currentUser);
    }

    [Fact]
    public async Task HandleAsync_Participant_SetsFlagAndSaves()
    {
        var conversation = Conversation.CreateDirect(_companyId, _userId, Guid.NewGuid());
        _conversations.GetByIdAsync(conversation.Id).Returns(conversation);

        await _sut.HandleAsync(new MarkConversationUnreadCommand(conversation.Id));

        Assert.True(conversation.Participants.Single(p => p.UserId == _userId).IsMarkedUnread);
        await _conversations.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task HandleAsync_NonParticipant_Throws()
    {
        var conversation = Conversation.CreateDirect(_companyId, Guid.NewGuid(), Guid.NewGuid());
        _conversations.GetByIdAsync(conversation.Id).Returns(conversation);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandleAsync(new MarkConversationUnreadCommand(conversation.Id)));
    }
}
