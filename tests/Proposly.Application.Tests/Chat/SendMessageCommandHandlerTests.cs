using NSubstitute;
using Proposly.Application.Abstractions;
using Proposly.Application.Chat.Commands.SendMessage;
using Proposly.Domain.Chat.Entities;
using Proposly.Domain.Chat.Repositories;
using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.CompanyManagement.Repositories;

namespace Proposly.Application.Tests.Chat;

public sealed class SendMessageCommandHandlerTests
{
    private readonly IConversationRepository _conversations = Substitute.For<IConversationRepository>();
    private readonly IChatMessageRepository _messages = Substitute.For<IChatMessageRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly SendMessageCommandHandler _sut;

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();

    public SendMessageCommandHandlerTests()
    {
        _currentUser.CompanyId.Returns(_companyId);
        _currentUser.UserId.Returns(_userId);
        _sut = new SendMessageCommandHandler(_conversations, _messages, _users, _currentUser);
    }

    private Conversation MakeConversation() => Conversation.CreateDirect(_companyId, _userId, _otherUserId);

    private User MakeUser() => User.Create(_companyId, "anna@test.com", "hash", "Anna", "Tester");

    [Fact]
    public async Task HandleAsync_ValidText_PersistsAndSnapshotsAuthorName()
    {
        var conversation = MakeConversation();
        _conversations.GetByIdAsync(conversation.Id).Returns(conversation);
        _users.GetByIdAsync(_userId).Returns(MakeUser());
        _messages.GetPendingAttachmentsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), conversation.Id)
            .Returns([]);

        var result = await _sut.HandleAsync(new SendMessageCommand(conversation.Id, "Hello!", []));

        Assert.Equal("Hello!", result.Body);
        Assert.Equal("Anna Tester", result.AuthorName);
        await _messages.Received(1).AddAsync(Arg.Is<ChatMessage>(m => m.Body == "Hello!"));
        await _messages.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task HandleAsync_SendingMarksConversationReadForSender()
    {
        var conversation = MakeConversation();
        _conversations.GetByIdAsync(conversation.Id).Returns(conversation);
        _users.GetByIdAsync(_userId).Returns(MakeUser());
        _messages.GetPendingAttachmentsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), conversation.Id)
            .Returns([]);

        await _sut.HandleAsync(new SendMessageCommand(conversation.Id, "Hi", []));

        var participant = conversation.Participants.Single(p => p.UserId == _userId);
        Assert.NotNull(participant.LastReadAt);
    }

    [Fact]
    public async Task HandleAsync_NonParticipant_Throws()
    {
        var conversation = Conversation.CreateDirect(_companyId, _otherUserId, Guid.NewGuid());
        _conversations.GetByIdAsync(conversation.Id).Returns(conversation);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandleAsync(new SendMessageCommand(conversation.Id, "Hi", [])));
    }

    [Fact]
    public async Task HandleAsync_UnknownConversation_Throws()
    {
        _conversations.GetByIdAsync(Arg.Any<Guid>()).Returns((Conversation?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandleAsync(new SendMessageCommand(Guid.NewGuid(), "Hi", [])));
    }

    [Fact]
    public async Task HandleAsync_MissingPendingAttachment_Throws()
    {
        var conversation = MakeConversation();
        _conversations.GetByIdAsync(conversation.Id).Returns(conversation);
        _users.GetByIdAsync(_userId).Returns(MakeUser());
        _messages.GetPendingAttachmentsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), conversation.Id)
            .Returns([]); // requested attachment does not come back

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandleAsync(new SendMessageCommand(conversation.Id, null, [Guid.NewGuid()])));
    }

    [Fact]
    public async Task HandleAsync_WithAttachments_BindsThemToTheMessage()
    {
        var conversation = MakeConversation();
        var attachment = ChatAttachment.Create(_companyId, conversation.Id, _userId, "plan.pdf", "application/pdf", 100, "key");
        _conversations.GetByIdAsync(conversation.Id).Returns(conversation);
        _users.GetByIdAsync(_userId).Returns(MakeUser());
        _messages.GetPendingAttachmentsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), conversation.Id)
            .Returns([attachment]);

        var result = await _sut.HandleAsync(new SendMessageCommand(conversation.Id, null, [attachment.Id]));

        Assert.NotNull(attachment.MessageId);
        Assert.Single(result.Attachments);
        Assert.Equal("plan.pdf", result.Attachments[0].FileName);
    }
}
