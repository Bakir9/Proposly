using Proposly.Domain.ProjectManagement.Entities;

namespace Proposly.Domain.Tests.ProjectManagement;

public class TaskTemplateTests
{
    [Fact]
    public void AddStep_AssignsSortOrderInSequence()
    {
        var template = TaskTemplate.Create(Guid.NewGuid(), "Card reader setup");

        var first = template.AddStep("Buy reader", 1m);
        var second = template.AddStep("Prepare for client", 2m);

        Assert.Equal(0, first.SortOrder);
        Assert.Equal(1, second.SortOrder);
        Assert.Equal(2, template.Steps.Count);
    }

    [Fact]
    public void AddStep_NegativeEstimate_Throws()
    {
        var template = TaskTemplate.Create(Guid.NewGuid(), "T");

        Assert.Throws<InvalidOperationException>(() => template.AddStep("Step", -1m));
    }

    [Fact]
    public void TotalEstimatedHours_SumsOnlyEstimatedSteps()
    {
        var template = TaskTemplate.Create(Guid.NewGuid(), "T");
        template.AddStep("Buy reader", 1.5m);
        template.AddStep("Test reader", 2m);
        template.AddStep("Client confirmation", null);

        Assert.Equal(3.5m, template.TotalEstimatedHours());
    }

    [Fact]
    public void TotalEstimatedHours_NoEstimates_ReturnsNull()
    {
        var template = TaskTemplate.Create(Guid.NewGuid(), "T");
        template.AddStep("Step without estimate", null);

        Assert.Null(template.TotalEstimatedHours());
    }
}
