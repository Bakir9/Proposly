using Proposly.Domain.ProjectManagement.Enums;
using Proposly.Domain.ProjectManagement.Entities;
using Proposly.Shared.ValueObjects;

namespace Proposly.Domain.Tests.ProjectManagement;

public class ProjectTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly DateOnly StartDate = new DateOnly(2025, 1, 1);
    private static readonly DateOnly Deadline = new DateOnly(2025, 12, 31);

    private static Project CreateProject() =>
        Project.Create(CompanyId, "Test Project", "Description", Guid.NewGuid(), "ACME Corp",
            new Money(50_000m, "EUR"), StartDate, Deadline);

    // --- AddMember ---

    [Fact]
    public void AddMember_NewUser_AddsMember()
    {
        var project = CreateProject();
        var userId = Guid.NewGuid();

        var member = project.AddMember(userId, "Alice", "Developer", new Money(80m, "EUR"));

        Assert.Single(project.Members);
        Assert.Equal(userId, member.UserId);
        Assert.Equal("Alice", member.Name);
    }

    [Fact]
    public void AddMember_DuplicateUser_Throws()
    {
        var project = CreateProject();
        var userId = Guid.NewGuid();
        project.AddMember(userId, "Alice", "Developer", new Money(80m, "EUR"));

        Assert.Throws<InvalidOperationException>(() =>
            project.AddMember(userId, "Alice Again", "Designer", new Money(70m, "EUR")));
    }

    // --- AddTask ---

    [Fact]
    public void AddTask_AddsTaskToCollection()
    {
        var project = CreateProject();

        var task = project.AddTask("Implement login", null, 5m, StartDate, Deadline);

        Assert.Single(project.Tasks);
        Assert.Equal("Implement login", task.Title);
    }

    [Fact]
    public void AddTask_WithParent_SetsParentTaskId()
    {
        var project = CreateProject();
        var parent = project.AddTask("Prepare and send card reader", null, null, StartDate, Deadline);

        var step = project.AddTask("Buy reader", null, 1m, StartDate, Deadline, parentTaskId: parent.Id);

        Assert.Equal(parent.Id, step.ParentTaskId);
        Assert.Null(parent.ParentTaskId);
    }

    [Fact]
    public void AddTask_ParentNotInProject_Throws()
    {
        var project = CreateProject();

        Assert.Throws<InvalidOperationException>(() =>
            project.AddTask("Step", null, null, StartDate, Deadline, parentTaskId: Guid.NewGuid()));
    }

    [Fact]
    public void AddTask_StepOfAStep_Throws()
    {
        var project = CreateProject();
        var parent = project.AddTask("Parent", null, null, StartDate, Deadline);
        var step = project.AddTask("Step", null, null, StartDate, Deadline, parentTaskId: parent.Id);

        Assert.Throws<InvalidOperationException>(() =>
            project.AddTask("Step of step", null, null, StartDate, Deadline, parentTaskId: step.Id));
    }

    [Fact]
    public void AddTask_Steps_GetSequentialSortOrder()
    {
        var project = CreateProject();
        var parent = project.AddTask("Parent", null, null, StartDate, Deadline);

        var first = project.AddTask("Step 1", null, null, StartDate, Deadline, parentTaskId: parent.Id);
        var second = project.AddTask("Step 2", null, null, StartDate, Deadline, parentTaskId: parent.Id);

        Assert.Equal(0, first.SortOrder);
        Assert.Equal(1, second.SortOrder);
    }

    // --- RemoveTask ---

    [Fact]
    public void RemoveTask_Parent_RemovesItsStepsToo()
    {
        var project = CreateProject();
        var parent = project.AddTask("Parent", null, null, StartDate, Deadline);
        project.AddTask("Step", null, null, StartDate, Deadline, parentTaskId: parent.Id);
        var other = project.AddTask("Other", null, null, StartDate, Deadline);

        project.RemoveTask(parent.Id);

        var remaining = Assert.Single(project.Tasks);
        Assert.Equal(other.Id, remaining.Id);
    }

    [Fact]
    public void RemoveTask_DetachesDependenciesOnRemovedTask()
    {
        var project = CreateProject();
        var blocker = project.AddTask("Blocker", null, null, StartDate, Deadline);
        var blocked = project.AddTask("Blocked", null, null, StartDate, Deadline);
        blocked.AddBlocker(blocker);

        project.RemoveTask(blocker.Id);

        Assert.Empty(blocked.BlockedBy);
    }

    [Fact]
    public void RemoveTask_NonExistentTask_Throws()
    {
        var project = CreateProject();

        Assert.Throws<InvalidOperationException>(() => project.RemoveTask(Guid.NewGuid()));
    }

    // --- UpdateTask ---

    [Fact]
    public void UpdateTask_ExistingTask_UpdatesFields()
    {
        var project = CreateProject();
        var task = project.AddTask("Old Title", null, 4m, StartDate, Deadline);

        project.UpdateTask(task.Id, "New Title", "New Desc", 8m, null, StartDate, Deadline, null, null);

        var updated = project.Tasks.First();
        Assert.Equal("New Title", updated.Title);
        Assert.Equal("New Desc", updated.Description);
        Assert.Equal(8m, updated.EstimatedHours);
    }

    [Fact]
    public void UpdateTask_NonExistentTask_Throws()
    {
        var project = CreateProject();

        Assert.Throws<InvalidOperationException>(() =>
            project.UpdateTask(Guid.NewGuid(), "Title", null, null, null, null, null, null, null));
    }

    // --- AddTaskComment ---

    [Fact]
    public void AddTaskComment_AddsCommentToTask()
    {
        var project = CreateProject();
        var task = project.AddTask("Task", null, null, null, null);
        var authorId = Guid.NewGuid();

        var comment = project.AddTaskComment(task.Id, authorId, "Bob", "Nice work!");

        Assert.Single(project.Tasks.First().Comments);
        Assert.Equal("Nice work!", comment.Body);
    }

    // --- EditTaskComment ---

    [Fact]
    public void EditTaskComment_ByAuthor_UpdatesBody()
    {
        var project = CreateProject();
        var task = project.AddTask("Task", null, null, null, null);
        var authorId = Guid.NewGuid();
        var comment = project.AddTaskComment(task.Id, authorId, "Bob", "Original");

        project.EditTaskComment(task.Id, comment.Id, authorId, "Revised");

        Assert.Equal("Revised", project.Tasks.First().Comments.First().Body);
    }

    // --- DeleteTaskComment ---

    [Fact]
    public void DeleteTaskComment_ByAdmin_RemovesComment()
    {
        var project = CreateProject();
        var task = project.AddTask("Task", null, null, null, null);
        var authorId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var comment = project.AddTaskComment(task.Id, authorId, "Bob", "A comment");

        project.DeleteTaskComment(task.Id, comment.Id, adminId, isAdmin: true);

        Assert.Empty(project.Tasks.First().Comments);
    }

    // --- CalculateLaborCost ---

    [Fact]
    public void CalculateLaborCost_SumsTimeEntries()
    {
        var project = CreateProject();
        var member = project.AddMember(Guid.NewGuid(), "Alice", "Developer", new Money(100m, "EUR"));

        // 3h @ 100 + 5h @ 100 = 800
        project.LogTime(member.Id, 3m, "Day 1", new DateOnly(2025, 1, 2));
        project.LogTime(member.Id, 5m, "Day 2", new DateOnly(2025, 1, 3));

        var labor = project.CalculateLaborCost();

        Assert.Equal(800m, labor.Amount);
        Assert.Equal("EUR", labor.Currency);
    }

    // --- CalculateTotalCost ---

    [Fact]
    public void CalculateTotalCost_SumsLaborAndExpenses()
    {
        var project = CreateProject();
        var member = project.AddMember(Guid.NewGuid(), "Alice", "Developer", new Money(100m, "EUR"));

        project.LogTime(member.Id, 4m, "Work", new DateOnly(2025, 1, 2)); // 400
        project.AddExpense("Server", new Money(250m, "EUR"), ExpenseCategory.Equipment, new DateOnly(2025, 1, 5));

        var total = project.CalculateTotalCost();

        Assert.Equal(650m, total.Amount);
        Assert.Equal("EUR", total.Currency);
    }

    // --- CalculateProfitability ---

    [Fact]
    public void CalculateProfitability_ReturnsRevenueMinusCost()
    {
        var project = CreateProject(); // budget = 50,000 EUR
        var member = project.AddMember(Guid.NewGuid(), "Alice", "Developer", new Money(100m, "EUR"));

        project.LogTime(member.Id, 10m, "Work", new DateOnly(2025, 1, 2)); // 1,000
        project.AddExpense("Tools", new Money(500m, "EUR"), ExpenseCategory.Materials, new DateOnly(2025, 1, 3)); // 500

        // total cost = 1,500; profitability = 50,000 - 1,500 = 48,500
        var profitability = project.CalculateProfitability();

        Assert.Equal(48_500m, profitability.Amount);
        Assert.Equal("EUR", profitability.Currency);
    }
}
