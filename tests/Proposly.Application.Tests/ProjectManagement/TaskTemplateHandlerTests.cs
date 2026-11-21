using NSubstitute;
using Proposly.Application.Abstractions;
using Proposly.Application.ProjectManagement.Commands.ApplyTaskTemplate;
using Proposly.Application.ProjectManagement.Commands.SaveTaskTemplate;
using Proposly.Domain.ProjectManagement.Entities;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Shared.ValueObjects;

namespace Proposly.Application.Tests.ProjectManagement;

public sealed class TaskTemplateHandlerTests
{
    private readonly IProjectRepository _projects = Substitute.For<IProjectRepository>();
    private readonly ITaskTemplateRepository _templates = Substitute.For<ITaskTemplateRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private static Project MakeProject()
    {
        return Project.Create(Guid.NewGuid(), "P", null, Guid.NewGuid(), "Client", Money.Zero("EUR"),
            DateOnly.FromDateTime(DateTime.UtcNow), null);
    }

    private static TaskTemplate MakeTemplate()
    {
        var template = TaskTemplate.Create(Guid.NewGuid(), "Prepare and send card reader", "Full rollout recipe");
        template.AddStep("Buy reader", 1m);
        template.AddStep("Prepare for client", 2m);
        template.AddStep("Client confirmation", null);
        return template;
    }

    // --- SaveTaskTemplate ---

    [Fact]
    public async Task SaveTaskTemplate_PersistsStepsInOrderForOwnCompany()
    {
        var companyId = Guid.NewGuid();
        _currentUser.CompanyId.Returns(companyId);
        var sut = new SaveTaskTemplateCommandHandler(_templates, _currentUser);
        var command = new SaveTaskTemplateCommand("Card reader rollout", null,
        [
            new SaveTaskTemplateStep("Buy reader", 1m),
            new SaveTaskTemplateStep("Test reader", null),
        ]);

        var id = await sut.HandleAsync(command);

        await _templates.Received(1).AddAsync(
            Arg.Is<TaskTemplate>(t =>
                t.Id == id &&
                t.CompanyId == companyId &&
                t.Steps.Count == 2 &&
                t.Steps.ElementAt(0).Title == "Buy reader" &&
                t.Steps.ElementAt(0).SortOrder == 0 &&
                t.Steps.ElementAt(1).Title == "Test reader" &&
                t.Steps.ElementAt(1).SortOrder == 1),
            Arg.Any<CancellationToken>());
    }

    // --- ApplyTaskTemplate ---

    [Fact]
    public async Task ApplyTaskTemplate_CreatesParentAndOneSubTaskPerStep()
    {
        var project = MakeProject();
        var template = MakeTemplate();
        _projects.GetByIdForWriteAsync(project.Id).Returns(project);
        _templates.GetByIdAsync(template.Id).Returns(template);
        var sut = new ApplyTaskTemplateCommandHandler(_projects, _templates);
        var assignee = Guid.NewGuid();

        var parentId = await sut.HandleAsync(
            new ApplyTaskTemplateCommand(project.Id, template.Id, assignee, null, null, null));

        var parent = Assert.Single(project.Tasks, t => t.ParentTaskId is null);
        Assert.Equal(parentId, parent.Id);
        Assert.Equal("Prepare and send card reader", parent.Title);
        Assert.Equal(3m, parent.EstimatedHours); // 1 + 2, unestimated step ignored
        Assert.Equal(assignee, parent.AssignedMemberId);

        var steps = project.Tasks.Where(t => t.ParentTaskId == parent.Id).OrderBy(t => t.SortOrder).ToList();
        Assert.Equal(3, steps.Count);
        Assert.Equal(["Buy reader", "Prepare for client", "Client confirmation"], steps.Select(s => s.Title));
        Assert.Equal(1m, steps[0].EstimatedHours);
        Assert.Null(steps[2].EstimatedHours);

        await _projects.Received(1).UpdateAsync(project, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyTaskTemplate_UnknownTemplate_Throws()
    {
        var project = MakeProject();
        _projects.GetByIdForWriteAsync(project.Id).Returns(project);
        _templates.GetByIdAsync(Arg.Any<Guid>()).Returns((TaskTemplate?)null);
        var sut = new ApplyTaskTemplateCommandHandler(_projects, _templates);

        await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            sut.HandleAsync(new ApplyTaskTemplateCommand(project.Id, Guid.NewGuid(), null, null, null, null)));
    }
}
