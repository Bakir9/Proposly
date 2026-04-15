using Proposly.Domain.ProjectManagement.Enums;
using Proposly.Domain.ProjectManagement.Events;
using Proposly.Shared.Interfaces;
using Proposly.Shared.Primitives;
using Proposly.Shared.ValueObjects;

namespace Proposly.Domain.ProjectManagement.Entities;

public sealed class Project : AggregateRoot<Guid>, ITenantEntity, IAuditableEntity
{
    private readonly List<ProjectMember> _members = [];
    private readonly List<ProjectTask> _tasks = [];
    private readonly List<Milestone> _milestones = [];
    private readonly List<Expense> _expenses = [];
    private readonly List<TimeEntry> _timeEntries = [];

    private Project() { } // For EF Core

    private Project(
        Guid id,
        Guid companyId,
        string name,
        string? description,
        string clientName,
        Money budget,
        DateOnly startDate,
        DateOnly? deadline)
        : base(id)
    {
        CompanyId = companyId;
        Name = name;
        Description = description;
        ClientName = clientName;
        Budget = budget;
        StartDate = startDate;
        Deadline = deadline;
        Status = ProjectStatus.Planning;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Project Create(
        Guid companyId,
        string name,
        string? description,
        string clientName,
        Money budget,
        DateOnly startDate,
        DateOnly? deadline)
    {
        var project = new Project(Guid.NewGuid(), companyId, name, description, clientName, budget, startDate, deadline);
        project.RaiseDomainEvent(new ProjectCreatedDomainEvent(project.Id, companyId));
        return project;
    }

    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string ClientName { get; private set; } = string.Empty;
    public Guid? LinkedOfferId { get; private set; }
    public Money? OfferedAmount { get; private set; }
    public Money Budget { get; private set; } = null!;
    public DateOnly StartDate { get; private set; }
    public DateOnly? Deadline { get; private set; }
    public ProjectStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyCollection<ProjectMember> Members => _members.AsReadOnly();
    public IReadOnlyCollection<ProjectTask> Tasks => _tasks.AsReadOnly();
    public IReadOnlyCollection<Milestone> Milestones => _milestones.AsReadOnly();
    public IReadOnlyCollection<Expense> Expenses => _expenses.AsReadOnly();
    public IReadOnlyCollection<TimeEntry> TimeEntries => _timeEntries.AsReadOnly();

    // --- Status transitions ---

    public void Activate()
    {
        if (Status is not (ProjectStatus.Planning or ProjectStatus.OnHold))
            throw new InvalidOperationException($"Cannot activate a project in '{Status}' status.");
        Status = ProjectStatus.Active;
        Touch();
        RaiseDomainEvent(new ProjectStatusChangedDomainEvent(Id, ProjectStatus.Active));
    }

    public void PutOnHold()
    {
        if (Status != ProjectStatus.Active)
            throw new InvalidOperationException("Only active projects can be put on hold.");
        Status = ProjectStatus.OnHold;
        Touch();
        RaiseDomainEvent(new ProjectStatusChangedDomainEvent(Id, ProjectStatus.OnHold));
    }

    public void Complete()
    {
        if (Status != ProjectStatus.Active)
            throw new InvalidOperationException("Only active projects can be completed.");
        Status = ProjectStatus.Completed;
        Touch();
        RaiseDomainEvent(new ProjectStatusChangedDomainEvent(Id, ProjectStatus.Completed));
    }

    public void Cancel()
    {
        if (Status is ProjectStatus.Completed or ProjectStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel a project in '{Status}' status.");
        Status = ProjectStatus.Cancelled;
        Touch();
        RaiseDomainEvent(new ProjectStatusChangedDomainEvent(Id, ProjectStatus.Cancelled));
    }

    // --- Domain operations ---

    public void LinkOffer(Guid offerId, Money offeredAmount)
    {
        LinkedOfferId = offerId;
        OfferedAmount = offeredAmount; // snapshot — never reference the live offer amount
        Touch();
    }

    public ProjectMember AddMember(Guid userId, string name, string role, Money hourlyRate)
    {
        if (_members.Any(m => m.UserId == userId))
            throw new InvalidOperationException($"User {userId} is already a member of this project.");

        var member = ProjectMember.Create(Id, userId, name, role, hourlyRate);
        _members.Add(member);
        Touch();
        return member;
    }

    public TimeEntry LogTime(Guid memberId, decimal hoursWorked, string? description, DateOnly date, Guid? taskId = null)
    {
        var member = _members.FirstOrDefault(m => m.Id == memberId)
            ?? throw new InvalidOperationException($"Member {memberId} is not part of this project.");

        // Create a NEW Money instance — never reuse member.HourlyRate directly.
        // EF Core cannot track the same object as an owned entity under two different parents.
        var rateSnapshot = new Money(member.HourlyRate.Amount, member.HourlyRate.Currency);
        var entry = TimeEntry.Create(Id, memberId, taskId, hoursWorked, rateSnapshot, description, date);
        _timeEntries.Add(entry);
        Touch();
        return entry;
    }

    public Expense AddExpense(string description, Money amount, ExpenseCategory category, DateOnly date)
    {
        var expense = Expense.Create(Id, description, amount, category, date);
        _expenses.Add(expense);
        Touch();
        return expense;
    }

    public ProjectTask AddTask(string title, string? description, decimal? estimatedHours, DateOnly? dueDate, Guid? milestoneId = null)
    {
        var task = ProjectTask.Create(Id, title, description, estimatedHours, dueDate, milestoneId);
        _tasks.Add(task);
        Touch();
        return task;
    }

    public Milestone AddMilestone(string title, DateOnly dueDate)
    {
        var milestone = Milestone.Create(Id, title, dueDate);
        _milestones.Add(milestone);
        Touch();
        return milestone;
    }

    // --- Financial calculations ---

    public Money CalculateLaborCost()
    {
        var zero = Money.Zero(Budget.Currency);
        return _timeEntries.Aggregate(zero, (total, entry) => total + entry.HourlyRateSnapshot * entry.HoursWorked);
    }

    public Money CalculateTotalCost()
    {
        var labor = CalculateLaborCost();
        return _expenses.Aggregate(labor, (total, expense) => total + expense.Amount);
    }

    public Money CalculateProfitability()
    {
        var revenue = OfferedAmount ?? Budget;
        return revenue - CalculateTotalCost();
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
