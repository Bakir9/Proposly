using Proposly.Domain.ProjectManagement.Enums;

namespace Proposly.Application.ProjectManagement.Responses;

public record ProjectDetailResponse(
    Guid Id, string Name, string? Description, Guid? ClientId, string ClientName, ProjectStatus Status,
    decimal BudgetAmount, string Currency, DateOnly StartDate, DateOnly? Deadline,
    Guid? LinkedOfferId, decimal? OfferedAmount,
    ProfitabilityResponse Profitability,
    IReadOnlyList<ProjectMemberResponse> Members,
    IReadOnlyList<ProjectTaskResponse> Tasks,
    IReadOnlyList<MilestoneResponse> Milestones,
    IReadOnlyList<ExpenseResponse> Expenses,
    IReadOnlyList<TimeEntryResponse> TimeEntries,
    IReadOnlyList<ProjectNoteResponse> Notes);

public record ProfitabilityResponse(decimal LaborCost, decimal ExpensesTotal, decimal TotalCost, decimal Revenue, decimal Profit, string Currency);
public record ProjectMemberResponse(Guid Id, Guid UserId, string Name, string Role, decimal HourlyRate, string Currency);
public record ProjectTaskResponse(Guid Id, string Title, string? Description, ProjectTaskStatus Status, decimal? EstimatedHours, decimal? ActualHours, DateOnly? StartDate, DateOnly? DueDate, DateOnly? CompletedAt, Guid? MilestoneId, Guid? AssignedMemberId, string? AssignedMemberName, IReadOnlyList<TaskCommentResponse> Comments, IReadOnlyList<Guid> BlockedByTaskIds);

public record BurndownDataPoint(DateOnly Date, int Count);
public record BurndownResponse(int TotalTasks, DateOnly StartDate, DateOnly EndDate, IReadOnlyList<BurndownDataPoint> Actual, IReadOnlyList<BurndownDataPoint> Ideal);

public record VelocityWeek(string WeekLabel, DateOnly WeekStart, int TasksCompleted, decimal HoursCompleted);
public record VelocityResponse(IReadOnlyList<VelocityWeek> Weeks, decimal TotalHoursCompleted, int TotalTasksCompleted);

public record MemberCapacity(string MemberName, decimal HoursLogged);
public record CapacityResponse(IReadOnlyList<MemberCapacity> Members, decimal TotalHoursLogged);
public record TaskCommentResponse(Guid Id, Guid AuthorId, string AuthorName, string Body, DateTime CreatedAt, DateTime? UpdatedAt);
public record ProjectNoteResponse(Guid Id, string Title, string Content, Guid AuthorId, string AuthorName, DateTime CreatedAt, DateTime UpdatedAt);
public record MilestoneResponse(Guid Id, string Title, DateOnly DueDate, bool IsCompleted);
public record ExpenseResponse(Guid Id, string Description, decimal Amount, string Currency, string Category, DateOnly Date);
public record TimeEntryResponse(Guid Id, Guid MemberId, string MemberName, decimal HoursWorked, decimal HourlyRateSnapshot, string Currency, string? Description, DateOnly Date, decimal Cost);
