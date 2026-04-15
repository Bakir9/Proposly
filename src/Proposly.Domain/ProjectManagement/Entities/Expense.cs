using Proposly.Domain.ProjectManagement.Enums;
using Proposly.Shared.Primitives;
using Proposly.Shared.ValueObjects;

namespace Proposly.Domain.ProjectManagement.Entities;

public sealed class Expense : Entity<Guid>
{
    private Expense() { } // For EF Core

    private Expense(Guid id, Guid projectId, string description, Money amount, ExpenseCategory category, DateOnly date)
        : base(id)
    {
        ProjectId = projectId;
        Description = description;
        Amount = amount;
        Category = category;
        Date = date;
    }

    public static Expense Create(Guid projectId, string description, Money amount, ExpenseCategory category, DateOnly date)
        => new(Guid.NewGuid(), projectId, description, amount, category, date);

    public Guid ProjectId { get; private set; }
    public string Description { get; private set; } = string.Empty;

    /// <summary>Snapshot of the cost at the time the expense was recorded.</summary>
    public Money Amount { get; private set; } = null!;
    public ExpenseCategory Category { get; private set; }
    public DateOnly Date { get; private set; }
}
