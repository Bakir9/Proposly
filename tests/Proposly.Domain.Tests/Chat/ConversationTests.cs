using Proposly.Domain.Chat.Entities;
using Proposly.Domain.Chat.Enums;

namespace Proposly.Domain.Tests.Chat;

public class ConversationTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid UserA = Guid.NewGuid();
    private static readonly Guid UserB = Guid.NewGuid();
    private static readonly Guid UserC = Guid.NewGuid();

    // --- CreateDirect ---

    [Fact]
    public void CreateDirect_TwoUsers_HasBothParticipantsAndNoTitle()
    {
        var conversation = Conversation.CreateDirect(CompanyId, UserA, UserB);

        Assert.Equal(ConversationKind.Direct, conversation.Kind);
        Assert.Null(conversation.Title);
        Assert.Equal(2, conversation.Participants.Count);
        Assert.True(conversation.IsParticipant(UserA));
        Assert.True(conversation.IsParticipant(UserB));
    }

    [Fact]
    public void CreateDirect_SameUserTwice_Throws()
        => Assert.Throws<InvalidOperationException>(() => Conversation.CreateDirect(CompanyId, UserA, UserA));

    // --- CreateGroup ---

    [Fact]
    public void CreateGroup_TitleAndTwoParticipants_Creates()
    {
        var conversation = Conversation.CreateGroup(CompanyId, "Offer review", [UserA, UserB]);

        Assert.Equal(ConversationKind.Group, conversation.Kind);
        Assert.Equal("Offer review", conversation.Title);
        Assert.Equal(2, conversation.Participants.Count);
    }

    [Fact]
    public void CreateGroup_WithoutTitle_Throws()
        => Assert.Throws<InvalidOperationException>(() => Conversation.CreateGroup(CompanyId, " ", [UserA, UserB]));

    [Fact]
    public void CreateGroup_FewerThanTwoParticipants_Throws()
        => Assert.Throws<InvalidOperationException>(() => Conversation.CreateGroup(CompanyId, "Lonely", [UserA]));

    [Fact]
    public void CreateGroup_DuplicateParticipants_AreDeduplicated()
    {
        var conversation = Conversation.CreateGroup(CompanyId, "Team", [UserA, UserA, UserB]);
        Assert.Equal(2, conversation.Participants.Count);
    }

    // --- CreateProjectChannel ---

    [Fact]
    public void CreateProjectChannel_SnapshotsProjectNameAsTitle()
    {
        var projectId = Guid.NewGuid();
        var conversation = Conversation.CreateProjectChannel(CompanyId, projectId, "Kitchen renovation");

        Assert.Equal(ConversationKind.Project, conversation.Kind);
        Assert.Equal(projectId, conversation.ProjectId);
        Assert.Equal("Kitchen renovation", conversation.Title);
        Assert.Empty(conversation.Participants);
    }

    // --- Participants ---

    [Fact]
    public void AddParticipant_OnDirect_Throws()
    {
        var conversation = Conversation.CreateDirect(CompanyId, UserA, UserB);
        Assert.Throws<InvalidOperationException>(() => conversation.AddParticipant(UserC));
    }

    [Fact]
    public void AddParticipant_Twice_KeepsOneRow()
    {
        var conversation = Conversation.CreateGroup(CompanyId, "Team", [UserA, UserB]);

        conversation.AddParticipant(UserC);
        conversation.AddParticipant(UserC);

        Assert.Equal(3, conversation.Participants.Count);
    }

    [Fact]
    public void RemoveParticipant_OnDirect_Throws()
    {
        var conversation = Conversation.CreateDirect(CompanyId, UserA, UserB);
        Assert.Throws<InvalidOperationException>(() => conversation.RemoveParticipant(UserB));
    }

    [Fact]
    public void EnsureParticipants_AddsOnlyMissingUsers()
    {
        var conversation = Conversation.CreateProjectChannel(CompanyId, Guid.NewGuid(), "P");
        conversation.AddParticipant(UserA);

        conversation.EnsureParticipants([UserA, UserB, UserC]);

        Assert.Equal(3, conversation.Participants.Count);
    }

    [Fact]
    public void EnsureParticipants_NeverRemovesExistingParticipants()
    {
        var conversation = Conversation.CreateProjectChannel(CompanyId, Guid.NewGuid(), "P");
        conversation.EnsureParticipants([UserA, UserB]);

        conversation.EnsureParticipants([UserC]);

        Assert.True(conversation.IsParticipant(UserA));
        Assert.True(conversation.IsParticipant(UserB));
        Assert.True(conversation.IsParticipant(UserC));
    }

    // --- MarkRead ---

    [Fact]
    public void MarkRead_AsParticipant_SetsLastRead()
    {
        var conversation = Conversation.CreateDirect(CompanyId, UserA, UserB);
        var messageId = Guid.NewGuid();

        conversation.MarkRead(UserA, messageId);

        var participant = conversation.Participants.Single(p => p.UserId == UserA);
        Assert.NotNull(participant.LastReadAt);
        Assert.Equal(messageId, participant.LastReadMessageId);
    }

    [Fact]
    public void MarkRead_AsNonParticipant_Throws()
    {
        var conversation = Conversation.CreateDirect(CompanyId, UserA, UserB);
        Assert.Throws<InvalidOperationException>(() => conversation.MarkRead(UserC, null));
    }

    // --- MarkUnread ---

    [Fact]
    public void MarkUnread_AsParticipant_SetsFlag()
    {
        var conversation = Conversation.CreateDirect(CompanyId, UserA, UserB);

        conversation.MarkUnread(UserA);

        Assert.True(conversation.Participants.Single(p => p.UserId == UserA).IsMarkedUnread);
        Assert.False(conversation.Participants.Single(p => p.UserId == UserB).IsMarkedUnread);
    }

    [Fact]
    public void MarkRead_ClearsTheManualUnreadFlag()
    {
        var conversation = Conversation.CreateDirect(CompanyId, UserA, UserB);
        conversation.MarkUnread(UserA);

        conversation.MarkRead(UserA, null);

        Assert.False(conversation.Participants.Single(p => p.UserId == UserA).IsMarkedUnread);
    }

    [Fact]
    public void MarkUnread_AsNonParticipant_Throws()
    {
        var conversation = Conversation.CreateDirect(CompanyId, UserA, UserB);
        Assert.Throws<InvalidOperationException>(() => conversation.MarkUnread(UserC));
    }

    // --- EnsureDeletableBy ---

    [Fact]
    public void EnsureDeletableBy_ParticipantOnDirect_Passes()
    {
        var conversation = Conversation.CreateDirect(CompanyId, UserA, UserB);
        conversation.EnsureDeletableBy(UserA); // no throw
    }

    [Fact]
    public void EnsureDeletableBy_ProjectChannel_Throws()
    {
        var conversation = Conversation.CreateProjectChannel(CompanyId, Guid.NewGuid(), "P");
        conversation.EnsureParticipants([UserA]);
        Assert.Throws<InvalidOperationException>(() => conversation.EnsureDeletableBy(UserA));
    }

    [Fact]
    public void EnsureDeletableBy_NonParticipant_Throws()
    {
        var conversation = Conversation.CreateDirect(CompanyId, UserA, UserB);
        Assert.Throws<InvalidOperationException>(() => conversation.EnsureDeletableBy(UserC));
    }
}
