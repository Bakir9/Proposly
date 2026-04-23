using Proposly.Domain.ProjectManagement.Enums;

namespace Proposly.Application.ProjectManagement.Responses;

public record ProjectDetailResponse(
    Guid Id, string Name, string? Description, string ClientName, ProjectStatus Status,
    decimal BudgetAmount, string Currency, DateOnly StartDate, DateOnly? Deadline,
    Guid? LinkedOfferId, decimal? OfferedAmount,
    ProfitabilityResponse Profitability,
    IReadOnlyList<ProjectMemberResponse> Members,
    IReadOnlyList<ProjectTaskResponse> Tasks,
    IReadOnlyList<MilestoneResponse> Milestones,
    IReadOnlyList<ExpenseResponse> Expenses,
    IReadOnlyList<TimeEntryResponse> TimeEntries);

public record ProfitabilityResponse(decimal LaborCost, decimal ExpensesTotal, decimal TotalCost, decimal Revenue, decimal Profit, string Currency);
public record ProjectMemberResponse(Guid Id, Guid UserId, string Name, string Role, decimal HourlyRate, string Currency);
public record ProjectTaskResponse(Guid Id, string Title, string? Description, ProjectTaskStatus Status, decimal? EstimatedHours, DateOnly? StartDate, DateOnly? DueDate, DateOnly? CompletedAt, Guid? MilestoneId, Guid? AssignedMemberId, string? AssignedMemberName, IReadOnlyList<TaskCommentResponse> Comments);

public record BurndownDataPoint(DateOnly Date, int Count);
public record BurndownResponse(int TotalTasks, DateOnly StartDate, DateOnly EndDate, IReadOnlyList<BurndownDataPoint> Actual, IReadOnlyList<BurndownDataPoint> Ideal);
public record TaskCommentResponse(Guid Id, Guid AuthorId, string AuthorName, string Body, DateTime CreatedAt, DateTime? UpdatedAt);
public record MilestoneResponse(Guid Id, string Title, DateOnly DueDate, bool IsCompleted);
public record ExpenseResponse(Guid Id, string Description, decimal Amount, string Currency, string Category, DateOnly Date);
public record TimeEntryResponse(Guid Id, Guid MemberId, string MemberName, decimal HoursWorked, decimal HourlyRateSnapshot, string Currency, string? Description, DateOnly Date, decimal Cost);
