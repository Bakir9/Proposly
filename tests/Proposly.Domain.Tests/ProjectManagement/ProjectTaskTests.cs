using Proposly.Domain.ProjectManagement.Entities;
using Proposly.Domain.ProjectManagement.Enums;

namespace Proposly.Domain.Tests.ProjectManagement;

public class ProjectTaskTests
{
    private static readonly Guid ProjectId = Guid.NewGuid();

    private static ProjectTask CreateTask() =>
        ProjectTask.Create(ProjectId, "Test Task", "Description", 8m,
            new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 10));

    // --- Start ---

    [Fact]
    public void Start_WhenTodo_TransitionsToInProgress()
    {
        var task = CreateTask();

        task.Start();

        Assert.Equal(ProjectTaskStatus.InProgress, task.Status);
    }

    [Fact]
    public void Start_WhenAlreadyInProgress_Throws()
    {
        var task = CreateTask();
        task.Start();

        Assert.Throws<InvalidOperationException>(() => task.Start());
    }

    // --- MoveToReview ---

    [Fact]
    public void MoveToReview_WhenInProgress_TransitionsToInReview()
    {
        var task = CreateTask();
        task.Start();

        task.MoveToReview(3m);

        Assert.Equal(ProjectTaskStatus.InReview, task.Status);
        Assert.Equal(3m, task.ActualHours);
    }

    [Fact]
    public void MoveToReview_WhenAlreadyInReview_Throws()
    {
        var task = CreateTask();
        task.Start();
        task.MoveToReview(3m);

        Assert.Throws<InvalidOperationException>(() => task.MoveToReview(3m));
    }

    // --- Complete ---

    [Fact]
    public void Complete_WithActualHours_TransitionsToDone()
    {
        var task = CreateTask();
        task.Start();

        task.Complete(5m);

        Assert.Equal(ProjectTaskStatus.Done, task.Status);
        Assert.Equal(5m, task.ActualHours);
    }

    [Fact]
    public void Complete_AfterReview_UsesExistingActualHours()
    {
        var task = CreateTask();
        task.Start();
        task.MoveToReview(3m);

        task.Complete();

        Assert.Equal(ProjectTaskStatus.Done, task.Status);
        Assert.Equal(3m, task.ActualHours);
    }

    [Fact]
    public void Complete_WithoutActualHours_Throws()
    {
        var task = CreateTask();
        task.Start();

        Assert.Throws<InvalidOperationException>(() => task.Complete());
    }

    [Fact]
    public void Complete_WhenAlreadyDone_Throws()
    {
        var task = CreateTask();
        task.Start();
        task.Complete(5m);

        Assert.Throws<InvalidOperationException>(() => task.Complete(5m));
    }

    // --- Reopen ---

    [Fact]
    public void Reopen_WhenDone_TransitionsToTodo()
    {
        var task = CreateTask();
        task.Start();
        task.Complete(5m);

        task.Reopen();

        Assert.Equal(ProjectTaskStatus.Todo, task.Status);
    }

    [Fact]
    public void Reopen_WhenAlreadyTodo_Throws()
    {
        var task = CreateTask();

        Assert.Throws<InvalidOperationException>(() => task.Reopen());
    }

    // --- UpdateDetails ---

    [Fact]
    public void UpdateDetails_UpdatesAllFields()
    {
        var task = CreateTask();
        var newStart = new DateOnly(2025, 2, 1);
        var newDue = new DateOnly(2025, 2, 15);
        var milestoneId = Guid.NewGuid();
        var assignedMemberId = Guid.NewGuid();

        task.UpdateDetails("New Title", "New Desc", 16m, newStart, newDue, milestoneId, assignedMemberId);

        Assert.Equal("New Title", task.Title);
        Assert.Equal("New Desc", task.Description);
        Assert.Equal(16m, task.EstimatedHours);
        Assert.Equal(newStart, task.StartDate);
        Assert.Equal(newDue, task.DueDate);
        Assert.Equal(milestoneId, task.MilestoneId);
        Assert.Equal(assignedMemberId, task.AssignedMemberId);
    }

    // --- AddComment ---

    [Fact]
    public void AddComment_AddsCommentToCollection()
    {
        var task = CreateTask();
        var authorId = Guid.NewGuid();

        task.AddComment(authorId, "Alice", "Looks good!");

        Assert.Single(task.Comments);
        Assert.Equal("Looks good!", task.Comments.First().Body);
    }

    // --- EditComment ---

    [Fact]
    public void EditComment_ByAuthor_UpdatesBody()
    {
        var task = CreateTask();
        var authorId = Guid.NewGuid();
        var comment = task.AddComment(authorId, "Alice", "Original body");

        task.EditComment(comment.Id, authorId, "Updated body");

        Assert.Equal("Updated body", task.Comments.First().Body);
    }

    [Fact]
    public void EditComment_ByNonAuthor_Throws()
    {
        var task = CreateTask();
        var authorId = Guid.NewGuid();
        var nonAuthorId = Guid.NewGuid();
        var comment = task.AddComment(authorId, "Alice", "Original body");

        Assert.Throws<UnauthorizedAccessException>(() =>
            task.EditComment(comment.Id, nonAuthorId, "Hacked body"));
    }

    // --- DeleteComment ---

    [Fact]
    public void DeleteComment_ByAuthor_RemovesComment()
    {
        var task = CreateTask();
        var authorId = Guid.NewGuid();
        var comment = task.AddComment(authorId, "Alice", "To be deleted");

        task.DeleteComment(comment.Id, authorId, isAdmin: false);

        Assert.Empty(task.Comments);
    }

    [Fact]
    public void DeleteComment_ByAdminNonAuthor_RemovesComment()
    {
        var task = CreateTask();
        var authorId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var comment = task.AddComment(authorId, "Alice", "To be deleted");

        task.DeleteComment(comment.Id, adminId, isAdmin: true);

        Assert.Empty(task.Comments);
    }

    [Fact]
    public void DeleteComment_ByNonAuthorNonAdmin_Throws()
    {
        var task = CreateTask();
        var authorId = Guid.NewGuid();
        var randomUserId = Guid.NewGuid();
        var comment = task.AddComment(authorId, "Alice", "Protected comment");

        Assert.Throws<UnauthorizedAccessException>(() =>
            task.DeleteComment(comment.Id, randomUserId, isAdmin: false));
    }
}
