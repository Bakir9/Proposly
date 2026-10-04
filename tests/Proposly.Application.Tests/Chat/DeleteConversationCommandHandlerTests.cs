using NSubstitute;
using Proposly.Application.Abstractions;
using Proposly.Application.Chat.Commands.DeleteConversation;
using Proposly.Domain.Chat.Entities;
using Proposly.Domain.Chat.Repositories;

namespace Proposly.Application.Tests.Chat;

public sealed class DeleteConversationCommandHandlerTests
{
    private readonly IConversationRepository _conversations = Substitute.For<IConversationRepository>();
    private readonly IChatMessageRepository _messages = Substitute.For<IChatMessageRepository>();
    private readonly IFileStorage _fileStorage = Substitute.For<IFileStorage>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly DeleteConversationCommandHandler _sut;

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();

    public DeleteConversationCommandHandlerTests()
    {
        _currentUser.CompanyId.Returns(_companyId);
        _currentUser.UserId.Returns(_userId);
        _sut = new DeleteConversationCommandHandler(_conversations, _messages, _fileStorage, _currentUser);
    }

    [Fact]
    public async Task HandleAsync_Direct_DeletesConversationAttachmentsAndBlobs()
    {
        var conversation = Conversation.CreateDirect(_companyId, _userId, _otherUserId);
        _conversations.GetByIdAsync(conversation.Id).Returns(conversation);
        _messages.GetAttachmentStorageKeysAsync(conversation.Id).Returns(["key-1", "key-2"]);

        await _sut.HandleAsync(new DeleteConversationCommand(conversation.Id));

        await _fileStorage.Received(1).DeleteAsync("key-1");
        await _fileStorage.Received(1).DeleteAsync("key-2");
        await _messages.Received(1).RemoveAttachmentsForConversationAsync(conversation.Id);
        await _conversations.Received(1).RemoveAsync(conversation);
        await _conversations.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task HandleAsync_ProjectChannel_ThrowsAndDeletesNothing()
    {
        var conversation = Conversation.CreateProjectChannel(_companyId, Guid.NewGuid(), "P");
        conversation.EnsureParticipants([_userId]);
        _conversations.GetByIdAsync(conversation.Id).Returns(conversation);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandleAsync(new DeleteConversationCommand(conversation.Id)));

        await _conversations.DidNotReceive().RemoveAsync(Arg.Any<Conversation>());
    }

    [Fact]
    public async Task HandleAsync_NonParticipant_Throws()
    {
        var conversation = Conversation.CreateDirect(_companyId, _otherUserId, Guid.NewGuid());
        _conversations.GetByIdAsync(conversation.Id).Returns(conversation);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandleAsync(new DeleteConversationCommand(conversation.Id)));
    }

    [Fact]
    public async Task HandleAsync_UnknownConversation_Throws()
    {
        _conversations.GetByIdAsync(Arg.Any<Guid>()).Returns((Conversation?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandleAsync(new DeleteConversationCommand(Guid.NewGuid())));
    }
}
