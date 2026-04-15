using Proposly.Shared.Primitives;
using Proposly.Shared.ValueObjects;

namespace Proposly.Domain.ProjectManagement.Entities;

public sealed class ProjectMember : Entity<Guid>
{
    private ProjectMember() { } // For EF Core

    private ProjectMember(Guid id, Guid projectId, Guid userId, string name, string role, Money hourlyRate)
        : base(id)
    {
        ProjectId = projectId;
        UserId = userId;
        Name = name;
        Role = role;
        HourlyRate = hourlyRate;
    }

    public static ProjectMember Create(Guid projectId, Guid userId, string name, string role, Money hourlyRate)
        => new(Guid.NewGuid(), projectId, userId, name, role, hourlyRate);

    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;

    /// <summary>Snapshot of the hourly rate at the time the member was added to the project.</summary>
    public Money HourlyRate { get; private set; } = null!;

    public void UpdateRole(string role) => Role = role;
}
