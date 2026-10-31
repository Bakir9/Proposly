namespace Proposly.Application.ProjectManagement.Responses;

/// <summary>
/// Everything the redesigned project Overview needs in one call. Money fields are null when the
/// caller may not see finance data (Members see hours, never rates or profit).
/// </summary>
public sealed record ProjectOverviewResponse(
    string BillingType,
    string Currency,
    bool CanViewMoney,
    OverviewKpis Kpis,
    OverviewChecklist Checklist,
    OverviewProfitability? Profitability,
    IReadOnlyList<OverviewMilestone> Milestones,
    IReadOnlyList<OverviewTeamMember> Team,
    decimal PlannedHours);

public sealed record OverviewKpis(
    DateOnly StartDate,
    DateOnly? Deadline,
    int? DaysLeft,
    int? DayOfTotal,
    int? TotalDays,
    decimal BudgetAmount,
    decimal? SpentAmount,
    int TasksDone,
    int TasksTotal,
    int TasksWithoutDates,
    string? NextMilestoneTitle,
    DateOnly? NextMilestoneDue,
    int NextMilestoneLinkedTasks);

/// <summary>Raw counts for the "Get this project ready" card — the UI builds the wording.</summary>
public sealed record OverviewChecklist(
    int MembersCount,
    bool AllRatesSet,
    int MilestonesCount,
    int OpenTasks,
    int ScheduledOpenTasks,
    int EstimatedOpenTasks,
    int TimeEntriesCount);

/// <summary>Mirrors ProjectFinancialsResult; all values in the project currency.</summary>
public sealed record OverviewProfitability(
    decimal? Revenue,
    decimal RevenueToDate,
    decimal PlannedLaborCost,
    decimal PlannedHours,
    decimal LoggedHours,
    decimal ActualLaborCost,
    decimal ActualExpenses,
    decimal ActualCost,
    decimal ForecastCost,
    decimal Result,
    decimal? MarginPercent,
    bool IsProjected);

public sealed record OverviewMilestone(
    Guid Id,
    string Title,
    DateOnly DueDate,
    bool IsCompleted,
    int LinkedTasks,
    int DoneLinkedTasks);

public sealed record OverviewTeamMember(
    Guid MemberId,
    Guid UserId,
    string Name,
    string Role,
    decimal? CostRate,
    decimal? BillRate,
    decimal AllocatedHours);
