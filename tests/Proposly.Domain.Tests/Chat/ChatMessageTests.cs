using Proposly.Domain.Chat.Entities;

namespace Proposly.Domain.Tests.Chat;

public class ChatMessageTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid ConversationId = Guid.NewGuid();
    private static readonly Guid AuthorId = Guid.NewGuid();

    [Fact]
    public void Create_WithBody_TrimsAndSnapshotsAuthorName()
    {
        var message = ChatMessage.Create(CompanyId, ConversationId, AuthorId, "Anna Tester", "  Hello  ", hasAttachments: false);

        Assert.Equal("Hello", message.Body);
        Assert.Equal("Anna Tester", message.AuthorName);
    }

    [Fact]
    public void Create_WithoutBodyAndWithoutAttachments_Throws()
        => Assert.Throws<InvalidOperationException>(
            () => ChatMessage.Create(CompanyId, ConversationId, AuthorId, "Anna", "  ", hasAttachments: false));

    [Fact]
    public void Create_AttachmentsOnly_HasNullBody()
    {
        var message = ChatMessage.Create(CompanyId, ConversationId, AuthorId, "Anna", null, hasAttachments: true);
        Assert.Null(message.Body);
    }
}

public class ChatAttachmentTests
{
    private static ChatAttachment NewAttachment(Guid uploader)
        => ChatAttachment.Create(Guid.NewGuid(), Guid.NewGuid(), uploader, "plan.pdf", "application/pdf", 1234, Guid.NewGuid().ToString());

    [Fact]
    public void AttachTo_ByUploader_SetsMessageId()
    {
        var uploader = Guid.NewGuid();
        var attachment = NewAttachment(uploader);
        var messageId = Guid.NewGuid();

        attachment.AttachTo(messageId, uploader);

        Assert.Equal(messageId, attachment.MessageId);
    }

    [Fact]
    public void AttachTo_Twice_Throws()
    {
        var uploader = Guid.NewGuid();
        var attachment = NewAttachment(uploader);
        attachment.AttachTo(Guid.NewGuid(), uploader);

        Assert.Throws<InvalidOperationException>(() => attachment.AttachTo(Guid.NewGuid(), uploader));
    }

    [Fact]
    public void AttachTo_ByAnotherUser_Throws()
    {
        var attachment = NewAttachment(Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(() => attachment.AttachTo(Guid.NewGuid(), Guid.NewGuid()));
    }
}
