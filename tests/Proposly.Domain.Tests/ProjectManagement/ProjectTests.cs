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
