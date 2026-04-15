using Proposly.Domain.CompanyManagement.Enums;
using Proposly.Shared.Interfaces;
using Proposly.Shared.Primitives;

namespace Proposly.Domain.CompanyManagement.Entities;

public sealed class Company : AggregateRoot<Guid>, IAuditableEntity
{
    private Company() { } // For EF Core

    private Company(Guid id, string name) : base(id)
    {
        Name = name;
        Status = CompanyStatus.Active;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Company Create(string name) => new(Guid.NewGuid(), name);
    public static Company Create(Guid id, string name) => new(id, name);

    public string Name { get; private set; } = string.Empty;
    public CompanyStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public void Suspend()
    {
        if (Status == CompanyStatus.Suspended)
            throw new InvalidOperationException("Company is already suspended.");
        Status = CompanyStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reactivate()
    {
        Status = CompanyStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }
}
