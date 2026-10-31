namespace Proposly.Application.Dashboard.Responses;

/// <summary>
/// The redesigned home dashboard in one call. Money fields are null when the caller may not see
/// finance data; the Offers list is empty for non-admins (offers are admin-only data).
/// </summary>
public sealed record DashboardHomeResponse(
    bool CanViewMoney,
    string Currency,
    IReadOnlyList<AttentionItem> Attention,
    DashboardKpis Kpis,
    IReadOnlyList<DashboardOfferItem> Offers,
    IReadOnlyList<DashboardProjectItem> Projects,
    MyTimeSummary MyTime);

/// <summary>One "Needs your attention" card. Link is an app route the frontend navigates to.</summary>
public sealed record AttentionItem(
    string Kind,   // approvals | timesheet | offer_no_reply | milestone_overdue | offer_no_project | missing_contracts
    string Tone,   // blue | amber | red | violet
    string Title,
    string Sub,
    string Cta,
    string Link);

public sealed record DashboardKpis(
    decimal? WonThisYear,
    int AcceptedThisYear,
    decimal? WinRatePercent,
    decimal? OpenPipeline,
    int OpenOffersCount,
    int WaitingForReply,
    int ProjectsActive,
    int ProjectsInProgress,
    int ProjectsPlanning,
    int ProjectsAtRisk);

public sealed record DashboardOfferItem(
    Guid Id,
    string Title,
    string ClientName,
    decimal? Amount,
    string Status,
    int AgeDays,
    DateOnly? AcceptedOn,
    string NextStep); // FollowUp | CreateProject | FinishSend | Open

public sealed record DashboardProjectItem(
    Guid Id,
    string Name,
    string ClientName,
    string Status,          // Active | Planning | OnHold
    decimal? BudgetUsedPercent,
    int TasksDone,
    int TasksTotal,
    int SetupDone,          // of 5, for Planning rows
    string NextLabel,
    bool NextAtRisk,
    decimal? ContractValue);

public sealed record MyTimeSummary(
    decimal WeekHours,
    decimal? WeekTargetHours,
    string PreviousMonthName,
    string PreviousMonthStatus, // NotSubmitted | Submitted | Approved | Locked | NoData
    decimal? VacationLeftDays,
    bool HasContract);

// ── Who's out ────────────────────────────────────────────────────────────────

public sealed record WhosOutResponse(
    int TeamTotal,
    int InToday,
    int OutToday,
    int NotScheduledToday,
    bool CanSeeTypes,
    IReadOnlyList<WhosOutDay> PerDay,
    IReadOnlyList<WhosOutPerson> People);

public sealed record WhosOutDay(DateOnly Date, int OutCount, bool IsNonWorking);

/// <summary>Type is "Absent" for viewers without rights — redacted server-side, never in the UI.</summary>
public sealed record WhosOutPerson(
    Guid UserId,
    string Name,
    string Type,
    DateOnly StartDate,
    DateOnly EndDate,
    DateOnly? BackOn);
