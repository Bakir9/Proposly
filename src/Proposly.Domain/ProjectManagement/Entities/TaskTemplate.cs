using Proposly.Shared.Interfaces;
using Proposly.Shared.Primitives;

namespace Proposly.Domain.ProjectManagement.Entities;

/// <summary>
/// A reusable recipe for a recurring piece of work: one parent task plus its ordered steps
/// (e.g. "Prepare and send card reader" → buy, prepare, test, send, confirmation). Applying it
/// to a project creates the parent task and one sub-task per step. Company-scoped — usable in
/// every project.
/// </summary>
public sealed class TaskTemplate : AggregateRoot<Guid>, ITenantEntity, IAuditableEntity
{
    private readonly List<TaskTemplateStep> _steps = [];

    private TaskTemplate() { } // For EF Core

    private TaskTemplate(Guid id, Guid companyId, string name, string? description)
        : base(id)
    {
        CompanyId = companyId;
        Name = name;
        Description = description;
    }

    public static TaskTemplate Create(Guid companyId, string name, string? description = null)
        => new(Guid.NewGuid(), companyId, name, description);

    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyCollection<TaskTemplateStep> Steps => _steps.AsReadOnly();

    public TaskTemplateStep AddStep(string title, decimal? estimatedHours)
    {
        if (estimatedHours is < 0)
            throw new InvalidOperationException("A step estimate cannot be negative.");
        var step = TaskTemplateStep.Create(Id, title, estimatedHours, _steps.Count);
        _steps.Add(step);
        return step;
    }

    public void Rename(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    /// <summary>Total of the step estimates — the template's expected effort.</summary>
    public decimal? TotalEstimatedHours()
    {
        var estimates = _steps.Where(s => s.EstimatedHours.HasValue).ToList();
        return estimates.Count == 0 ? null : estimates.Sum(s => s.EstimatedHours!.Value);
    }
}
