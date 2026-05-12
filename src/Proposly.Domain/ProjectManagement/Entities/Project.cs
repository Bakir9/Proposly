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
    private readonly List<ProjectNote> _notes = [];

    private Project() { } // For EF Core

    private Project(
        Guid id,
        Guid companyId,
        string name,
        string? description,
        Guid clientId,
        string clientName,
        Money budget,
        DateOnly startDate,
        DateOnly? deadline)
        : base(id)
    {
        CompanyId = companyId;
        Name = name;
        Description = description;
        ClientId = clientId;
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
        Guid clientId,
        string clientName,
        Money budget,
        DateOnly startDate,
        DateOnly? deadline)
    {
        var project = new Project(Guid.NewGuid(), companyId, name, description, clientId, clientName, budget, startDate, deadline);
        project.RaiseDomainEvent(new ProjectCreatedDomainEvent(project.Id, companyId));
        return project;
    }

    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? ClientId { get; private set; }
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
    public IReadOnlyCollection<ProjectNote> Notes => _notes.AsReadOnly();

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

    public void UpdateDetails(string name, string? description, DateOnly? deadline, Money? budget = null)
    {
        Name = name;
        Description = description;
        Deadline = deadline;
        if (budget is not null)
            Budget = budget;
        Touch();
    }

    public void UpdateClient(Guid clientId, string clientNameSnapshot)
    {
        ClientId = clientId;
        ClientName = clientNameSnapshot;
        Touch();
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

    public void CompleteTask(Guid taskId, decimal? actualHours)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == taskId)
            ?? throw new InvalidOperationException($"Task {taskId} not found in project.");

        task.Complete(actualHours);

        if (!task.AssignedMemberId.HasValue)
        {
            Touch();
            return;
        }

        var hoursToLog = task.ActualHours ?? task.EstimatedHours;
        if (!hoursToLog.HasValue)
        {
            Touch();
            return;
        }

        var member = _members.FirstOrDefault(m => m.Id == task.AssignedMemberId.Value);
        if (member is not null)
        {
            var desc = BuildAutoLogDescription(task);
            var existing = _timeEntries.FirstOrDefault(te => te.TaskId == taskId);
            if (existing is not null)
                existing.Update(hoursToLog.Value, desc);
            else
                LogTime(member.Id, hoursToLog.Value, desc, DateOnly.FromDateTime(DateTime.UtcNow), taskId);
        }

        Touch();
    }

    private static string BuildAutoLogDescription(ProjectTask task)
    {
        var hoursLogged = task.ActualHours ?? task.EstimatedHours;
        if (task.ActualHours.HasValue && task.EstimatedHours.HasValue)
        {
            var delta = task.ActualHours.Value - task.EstimatedHours.Value;
            var deltaStr = delta == 0 ? "on estimate"
                : delta > 0 ? $"+{delta:0.##}h over estimate"
                : $"{Math.Abs(delta):0.##}h under estimate";
            return $"[Auto] {task.Title} — est. {task.EstimatedHours:0.##}h / actual {task.ActualHours:0.##}h ({deltaStr})";
        }
        return $"[Auto] {task.Title}" + (hoursLogged.HasValue ? $" — {hoursLogged:0.##}h" : string.Empty);
    }

    public Expense AddExpense(string description, Money amount, ExpenseCategory category, DateOnly date)
    {
        var expense = Expense.Create(Id, description, amount, category, date);
        _expenses.Add(expense);
        Touch();
        return expense;
    }

    public ProjectTask AddTask(string title, string? description, decimal? estimatedHours, DateOnly? startDate, DateOnly? dueDate, Guid? milestoneId = null, Guid? assignedMemberId = null)
    {
        var task = ProjectTask.Create(Id, title, description, estimatedHours, startDate, dueDate, milestoneId, assignedMemberId);
        _tasks.Add(task);
        if (assignedMemberId.HasValue)
        {
            var member = _members.FirstOrDefault(m => m.Id == assignedMemberId.Value);
            if (member is not null)
                RaiseDomainEvent(new Events.TaskAssignedDomainEvent(task.Id, task.Title, Id, Name, assignedMemberId.Value, member.UserId, CompanyId));
        }
        Touch();
        return task;
    }

    public void UpdateTask(Guid taskId, string title, string? description, decimal? estimatedHours, DateOnly? startDate, DateOnly? dueDate, Guid? milestoneId, Guid? assignedMemberId)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == taskId)
            ?? throw new InvalidOperationException($"Task {taskId} not found in project.");

        var previousAssignee = task.AssignedMemberId;
        task.UpdateDetails(title, description, estimatedHours, startDate, dueDate, milestoneId, assignedMemberId);

        if (assignedMemberId.HasValue && assignedMemberId != previousAssignee)
        {
            var member = _members.FirstOrDefault(m => m.Id == assignedMemberId.Value);
            if (member is not null)
                RaiseDomainEvent(new Events.TaskAssignedDomainEvent(task.Id, task.Title, Id, Name, assignedMemberId.Value, member.UserId, CompanyId));
        }
        Touch();
    }

    public TaskComment AddTaskComment(Guid taskId, Guid authorId, string authorName, string body)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == taskId)
            ?? throw new InvalidOperationException($"Task {taskId} not found in project.");
        var comment = task.AddComment(authorId, authorName, body);
        Touch();
        return comment;
    }

    public void EditTaskComment(Guid taskId, Guid commentId, Guid editorId, string newBody)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == taskId)
            ?? throw new InvalidOperationException($"Task {taskId} not found in project.");
        task.EditComment(commentId, editorId, newBody);
        Touch();
    }

    public void DeleteTaskComment(Guid taskId, Guid commentId, Guid userId, bool isAdmin)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == taskId)
            ?? throw new InvalidOperationException($"Task {taskId} not found in project.");
        task.DeleteComment(commentId, userId, isAdmin);
        Touch();
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

    public ProjectNote AddNote(string title, string content, Guid authorId, string authorName)
    {
        var note = ProjectNote.Create(Id, title, content, authorId, authorName);
        _notes.Add(note);
        Touch();
        return note;
    }

    public void UpdateNote(Guid noteId, string title, string content)
    {
        var note = _notes.FirstOrDefault(n => n.Id == noteId)
            ?? throw new InvalidOperationException($"Note {noteId} not found.");
        note.Update(title, content);
        Touch();
    }

    public void DeleteNote(Guid noteId)
    {
        var note = _notes.FirstOrDefault(n => n.Id == noteId)
            ?? throw new InvalidOperationException($"Note {noteId} not found.");
        _notes.Remove(note);
        Touch();
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
