using Proposly.Shared.Primitives;

namespace Proposly.Domain.HrManagement.Entities;

/// <summary>Internal note on a worker — never visible to the worker themselves.</summary>
public sealed class EmployeeNote : Entity<Guid>
{
    public static readonly string[] Tags = ["General", "HR", "Contract", "Performance"];

    private EmployeeNote() { } // For EF Core

    private EmployeeNote(Guid id, Guid employeeProfileId, string tag, string text) : base(id)
    {
        EmployeeProfileId = employeeProfileId;
        Tag = tag;
        Text = text;
        CreatedAt = DateTime.UtcNow;
    }

    internal static EmployeeNote Create(Guid employeeProfileId, string tag, string text)
    {
        if (!Tags.Contains(tag))
            throw new InvalidOperationException($"Unknown note tag '{tag}'.");
        return new EmployeeNote(Guid.NewGuid(), employeeProfileId, tag, text);
    }

    public Guid EmployeeProfileId { get; private set; }
    public string Tag { get; private set; } = string.Empty;
    public string Text { get; private set; } = string.Empty;
    public bool Pinned { get; private set; }
    public DateTime CreatedAt { get; private set; }

    internal void SetPinned(bool pinned) => Pinned = pinned;
}
