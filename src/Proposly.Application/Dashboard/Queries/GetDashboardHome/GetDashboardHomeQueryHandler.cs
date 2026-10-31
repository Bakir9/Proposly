using System.Globalization;
using Proposly.Application.Abstractions;
using Proposly.Application.Dashboard.Responses;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.HrManagement.Repositories;
using Proposly.Domain.OfferManagement.Entities;
using Proposly.Domain.OfferManagement.Enums;
using Proposly.Domain.ProjectManagement.Entities;
using Proposly.Domain.ProjectManagement.Enums;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Domain.ProjectManagement.Services;
using Proposly.Domain.OfferManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.Dashboard.Queries.GetDashboardHome;

/// <summary>
/// Assembles the redesigned home dashboard (spec §§2–5, 7, 9). Money and offers are admin-only;
/// members get their projects without money, their own attention items and My time. "At risk" =
/// an overdue milestone, or (for admins) forecast cost above budget.
/// </summary>
public sealed class GetDashboardHomeQueryHandler(
    IOfferRepository offers,
    IClientRepository clients,
    IProjectRepository projects,
    ITimesheetRepository timesheets,
    IAbsenceRepository absences,
    IEmploymentTermsRepository terms,
    IEmployeeProfileRepository profiles,
    IUserRepository users,
    ICurrentUserService currentUser) : IQueryHandler<GetDashboardHomeQuery, DashboardHomeResponse>
{
    private const int SentReminderDays = 7;
    private const int StaleDraftDays = 14;
    private const int TimesheetDueDay = 5;
    private const int ContractExpiryWindowDays = 60;

    public async Task<DashboardHomeResponse> HandleAsync(GetDashboardHomeQuery query, CancellationToken ct = default)
    {
        var isAdmin = currentUser.CanViewAllEmployees;
        var today = DateOnly.FromDateTime(DateTime.Now);
        var now = DateTime.UtcNow;

        var allProjects = await projects.GetAllForDashboardAsync(ct);
        var allOffers = isAdmin ? await offers.GetAllAsync(ct) : [];
        var clientNames = isAdmin
            ? (await clients.GetAllAsync(ct)).ToDictionary(c => c.Id, c => c.Name)
            : new Dictionary<Guid, string>();
        var linkedOfferIds = allProjects.Where(p => p.LinkedOfferId.HasValue).Select(p => p.LinkedOfferId!.Value).ToHashSet();

        var currency = allProjects.FirstOrDefault()?.Budget.Currency
            ?? allOffers.FirstOrDefault()?.Currency ?? "EUR";

        var activeProjects = allProjects
            .Where(p => p.Status is ProjectStatus.Planning or ProjectStatus.Active or ProjectStatus.OnHold)
            .ToList();
        var financials = isAdmin
            ? activeProjects.ToDictionary(p => p.Id, ProjectFinancials.Calculate)
            : [];

        bool IsAtRisk(Project p) =>
            p.Milestones.Any(m => !m.IsCompleted && m.DueDate < today)
            || (financials.TryGetValue(p.Id, out var f) && f.ForecastCost > p.Budget.Amount);

        var attention = await BuildAttentionAsync(isAdmin, allOffers, clientNames, allProjects, today, now, ct);
        var kpis = BuildKpis(isAdmin, allOffers, activeProjects, IsAtRisk, now);
        var offerItems = BuildOffers(allOffers, clientNames, linkedOfferIds, now);
        var projectItems = BuildProjects(activeProjects, financials, IsAtRisk, isAdmin, today);
        var myTime = await BuildMyTimeAsync(today, ct);

        return new DashboardHomeResponse(isAdmin, currency, attention, kpis, offerItems, projectItems, myTime);
    }

    // ── Needs your attention (spec §2, priority order) ──────────────────────

    private async Task<IReadOnlyList<AttentionItem>> BuildAttentionAsync(
        bool isAdmin, IReadOnlyList<Offer> allOffers, IReadOnlyDictionary<Guid, string> clientNames, IReadOnlyList<Project> allProjects,
        DateOnly today, DateTime now, CancellationToken ct)
    {
        var items = new List<AttentionItem>();

        if (isAdmin)
        {
            var pendingSheets = (await timesheets.GetSubmittedAsync(ct)).Count;
            var pendingAbsences = (await absences.GetPendingAsync(ct)).Count;
            var total = pendingSheets + pendingAbsences;
            if (total > 0)
            {
                var parts = new List<string>();
                if (pendingSheets > 0) parts.Add($"{pendingSheets} timesheet{(pendingSheets > 1 ? "s" : "")}");
                if (pendingAbsences > 0) parts.Add($"{pendingAbsences} time off request{(pendingAbsences > 1 ? "s" : "")}");
                items.Add(new AttentionItem("approvals", "blue",
                    $"{total} approval{(total > 1 ? "s" : "")} waiting", string.Join(", ", parts),
                    "Open approvals", "/approvals"));
            }
        }

        // Own previous-month sheet still in Draft after the 1st.
        var prevMonth = today.AddMonths(-1);
        var ownPrev = await timesheets.GetForUserAsync(currentUser.UserId, prevMonth.Year, prevMonth.Month, ct);
        if (today.Day >= 1 && (ownPrev is null || ownPrev.Status == TimesheetStatus.Draft)
            && (ownPrev is not null || today.Day <= 20)) // no card for an empty month long past
        {
            var monthName = new DateOnly(prevMonth.Year, prevMonth.Month, 1).ToString("MMMM", CultureInfo.GetCultureInfo("en"));
            var due = new DateOnly(today.Year, today.Month, Math.Min(TimesheetDueDay, 28));
            items.Add(new AttentionItem("timesheet", "violet",
                $"Your {monthName} timesheet", $"Not submitted · due {due:dd.MM.}",
                "Submit month", "/redesign/working-time"));
        }

        if (isAdmin)
        {
            var waiting = allOffers
                .Where(o => o.Status == OfferStatus.Sent && o.SentAt.HasValue && (now - o.SentAt.Value).TotalDays >= SentReminderDays)
                .OrderBy(o => o.SentAt)
                .FirstOrDefault();
            if (waiting is not null)
            {
                var days = (int)(now - waiting.SentAt!.Value).TotalDays;
                items.Add(new AttentionItem("offer_no_reply", "amber",
                    $"{waiting.Title} — no reply for {days} days",
                    $"{clientNames.GetValueOrDefault(waiting.ClientId, "—")} · {waiting.CalculateVatBase().Amount:N0} · sent {DateOnly.FromDateTime(waiting.SentAt.Value):dd.MM.}",
                    "Follow up", $"/offers/{waiting.Id}"));
            }
        }

        var overdue = allProjects
            .Where(p => p.Status is ProjectStatus.Planning or ProjectStatus.Active)
            .SelectMany(p => p.Milestones.Where(m => !m.IsCompleted && m.DueDate < today).Select(m => (p, m)))
            .OrderBy(x => x.m.DueDate)
            .FirstOrDefault();
        if (overdue.m is not null)
        {
            items.Add(new AttentionItem("milestone_overdue", "red",
                $"Milestone “{overdue.m.Title}” overdue",
                $"due {overdue.m.DueDate:dd.MM.} · {overdue.p.Name}",
                "Open milestones", $"/projects/{overdue.p.Id}/plan/milestones"));
        }

        // Accepted-without-project is NOT a card: the Offers table already surfaces it with a
        // primary "Create project" button — one place is enough.

        // Sent offers whose validity runs out soon — expiry is not visible in the Offers table.
        if (isAdmin)
        {
            var expiring = allOffers
                .Where(o => o.Status == OfferStatus.Sent
                            && o.ValidUntil.HasValue
                            && o.ValidUntil.Value >= today
                            && o.ValidUntil.Value <= today.AddDays(7))
                .OrderBy(o => o.ValidUntil)
                .FirstOrDefault();
            if (expiring is not null)
            {
                var daysLeft = expiring.ValidUntil!.Value.DayNumber - today.DayNumber;
                items.Add(new AttentionItem("offer_expiring", "amber",
                    $"{expiring.Title} expires {(daysLeft == 0 ? "today" : $"in {daysLeft} day{(daysLeft > 1 ? "s" : "")}")}",
                    $"{clientNames.GetValueOrDefault(expiring.ClientId, "—")} · valid until {expiring.ValidUntil.Value:dd.MM.}",
                    "Follow up", $"/offers/{expiring.Id}"));
            }
        }

        // Fixed-term contracts running out within the next two months (HR, admins only) —
        // enough lead time to renew or start offboarding.
        if (isAdmin)
        {
            var expiryLimit = today.AddDays(ContractExpiryWindowDays);
            var expiringContracts = (await profiles.GetAllAsync(ct))
                .Where(p => p.EmploymentEnd.HasValue && p.EmploymentEnd.Value >= today && p.EmploymentEnd.Value <= expiryLimit)
                .OrderBy(p => p.EmploymentEnd)
                .ToList();
            if (expiringContracts.Count > 0)
            {
                var names = (await users.GetActiveAsync(ct)).ToDictionary(u => u.Id, u => u.FullName);
                var first = expiringContracts[0];
                var firstName = names.GetValueOrDefault(first.UserId, "an employee");
                items.Add(new AttentionItem("contract_expiring", "violet",
                    expiringContracts.Count == 1
                        ? $"Contract of {firstName} ends {first.EmploymentEnd!.Value:dd.MM.yyyy}"
                        : $"{expiringContracts.Count} contracts end within 2 months",
                    expiringContracts.Count == 1
                        ? "Renew it or plan the offboarding in time"
                        : $"Next: {firstName} on {first.EmploymentEnd!.Value:dd.MM.yyyy}",
                    "Open team settings", "/redesign/team"));
            }
        }

        // Overdue tasks across the projects the caller can see — actionable for everyone.
        var overdueTasks = allProjects
            .Where(p => p.Status is ProjectStatus.Planning or ProjectStatus.Active)
            .SelectMany(p => p.Tasks
                .Where(t => t.Status != ProjectTaskStatus.Done && t.DueDate.HasValue && t.DueDate.Value < today)
                .Select(t => (p, t)))
            .OrderBy(x => x.t.DueDate)
            .ToList();
        if (overdueTasks.Count > 0)
        {
            var (wp, wt) = overdueTasks[0];
            items.Add(new AttentionItem("tasks_overdue", "red",
                $"{overdueTasks.Count} task{(overdueTasks.Count > 1 ? "s" : "")} overdue",
                $"“{wt.Title}” in {wp.Name} · due {wt.DueDate:dd.MM.}",
                "Open board", $"/projects/{wp.Id}/plan/board"));
        }

        return items.Take(4).ToList();
    }

    // ── KPI row (spec §3) ────────────────────────────────────────────────────

    private static DashboardKpis BuildKpis(
        bool isAdmin, IReadOnlyList<Offer> allOffers, IReadOnlyList<Project> activeProjects,
        Func<Project, bool> isAtRisk, DateTime now)
    {
        decimal? won = null, pipeline = null, winRate = null;
        int acceptedCount = 0, openCount = 0, waiting = 0;

        if (isAdmin)
        {
            var year = now.Year;
            // Offers carry no explicit decision date; the status change stamps UpdatedAt.
            var accepted = allOffers.Where(o => o.Status == OfferStatus.Accepted && o.UpdatedAt.Year == year).ToList();
            var resolved = allOffers.Count(o =>
                o.Status is OfferStatus.Accepted or OfferStatus.Rejected or OfferStatus.Expired && o.UpdatedAt.Year == year);
            acceptedCount = accepted.Count;
            won = accepted.Sum(o => o.CalculateVatBase().Amount);
            winRate = resolved > 0 ? Math.Round(acceptedCount * 100m / resolved, 0) : null;

            var open = allOffers.Where(o => o.Status is OfferStatus.Draft or OfferStatus.Sent).ToList();
            openCount = open.Count;
            waiting = open.Count(o => o.Status == OfferStatus.Sent);
            pipeline = open.Sum(o => o.CalculateVatBase().Amount);
        }

        return new DashboardKpis(
            WonThisYear: won,
            AcceptedThisYear: acceptedCount,
            WinRatePercent: winRate,
            OpenPipeline: pipeline,
            OpenOffersCount: openCount,
            WaitingForReply: waiting,
            ProjectsActive: activeProjects.Count,
            ProjectsInProgress: activeProjects.Count(p => p.Status == ProjectStatus.Active),
            ProjectsPlanning: activeProjects.Count(p => p.Status == ProjectStatus.Planning),
            ProjectsAtRisk: activeProjects.Count(isAtRisk));
    }

    // ── Offers card (spec §4): urgency sort, next steps ──────────────────────

    private static IReadOnlyList<DashboardOfferItem> BuildOffers(
        IReadOnlyList<Offer> allOffers, IReadOnlyDictionary<Guid, string> clientNames, HashSet<Guid> linkedOfferIds, DateTime now)
    {
        int AgeDays(DateTime from) => Math.Max(0, (int)(now - from).TotalDays);

        var sorted = allOffers
            .Where(o => o.Status is OfferStatus.Sent or OfferStatus.Accepted or OfferStatus.Draft)
            .Select(o =>
            {
                var age = o.Status switch
                {
                    OfferStatus.Sent => AgeDays(o.SentAt ?? o.UpdatedAt),
                    OfferStatus.Draft => AgeDays(o.CreatedAt),
                    _ => AgeDays(o.UpdatedAt),
                };
                var needsProject = o.Status == OfferStatus.Accepted && !linkedOfferIds.Contains(o.Id);
                var urgency = o.Status switch
                {
                    OfferStatus.Sent => 0,
                    OfferStatus.Accepted when needsProject => 1,
                    OfferStatus.Draft => 2,
                    _ => 3,
                };
                var next = o.Status switch
                {
                    OfferStatus.Sent => "FollowUp",
                    OfferStatus.Accepted when needsProject => "CreateProject",
                    OfferStatus.Draft when age >= StaleDraftDays => "FinishSend",
                    _ => "Open",
                };
                return (o, age, urgency, next);
            })
            .OrderBy(x => x.urgency)
            .ThenByDescending(x => x.urgency == 0 ? x.age : 0)   // Sent waiting longest first
            .ThenByDescending(x => x.urgency == 2 ? x.age : 0)   // Drafts oldest first
            .Take(5)
            .Select(x => new DashboardOfferItem(
                x.o.Id, x.o.Title, clientNames.GetValueOrDefault(x.o.ClientId, "—"),
                x.o.CalculateVatBase().Amount, x.o.Status.ToString(), x.age,
                x.o.Status == OfferStatus.Accepted ? DateOnly.FromDateTime(x.o.UpdatedAt) : null,
                x.next))
            .ToList();

        return sorted;
    }

    // ── Projects card (spec §5) ──────────────────────────────────────────────

    private static IReadOnlyList<DashboardProjectItem> BuildProjects(
        IReadOnlyList<Project> activeProjects,
        IReadOnlyDictionary<Guid, ProjectFinancialsResult> financials,
        Func<Project, bool> isAtRisk, bool isAdmin, DateOnly today)
    {
        static int SetupDone(Project p)
        {
            var open = p.Tasks.Where(t => t.Status != ProjectTaskStatus.Done).ToList();
            var done = 0;
            if (p.Members.Count > 0 && p.Members.All(m => m.CostRate.Amount > 0)) done++;
            if (p.Milestones.Count > 0) done++;
            if (open.Count > 0 && open.All(t => t.StartDate is not null && t.DueDate is not null)) done++;
            if (open.Count > 0 && open.All(t => t.EstimatedHours is not null)) done++;
            if (p.TimeEntries.Count > 0) done++;
            return done;
        }

        return activeProjects
            .OrderByDescending(isAtRisk)
            .ThenByDescending(p => p.Status == ProjectStatus.Active)
            .ThenBy(p => p.Name)
            .Take(5)
            .Select(p =>
            {
                decimal? budgetUsed = null;
                if (isAdmin && financials.TryGetValue(p.Id, out var f) && p.Budget.Amount > 0)
                    budgetUsed = Math.Round(f.ActualCost / p.Budget.Amount * 100m, 0);

                var nextMilestone = p.Milestones.Where(m => !m.IsCompleted).OrderBy(m => m.DueDate).FirstOrDefault();
                string nextLabel;
                var nextAtRisk = false;
                if (nextMilestone is not null)
                {
                    nextAtRisk = nextMilestone.DueDate < today;
                    nextLabel = $"{nextMilestone.Title} {nextMilestone.DueDate:dd.MM.}{(nextAtRisk ? " · overdue" : "")}";
                }
                else
                {
                    var open = p.Tasks.Where(t => t.Status != ProjectTaskStatus.Done).ToList();
                    nextLabel = p.Members.Count == 0 || p.Members.Any(m => m.CostRate.Amount <= 0)
                        ? "Assign team & rates"
                        : open.Count == 0 || open.Any(t => t.StartDate is null || t.DueDate is null)
                            ? "Add tasks with dates"
                            : "Create a milestone";
                }

                return new DashboardProjectItem(
                    p.Id, p.Name, p.ClientName, p.Status.ToString(),
                    budgetUsed,
                    p.Tasks.Count(t => t.Status == ProjectTaskStatus.Done), p.Tasks.Count,
                    SetupDone(p),
                    nextLabel, nextAtRisk,
                    isAdmin ? (p.OfferedAmount ?? p.Budget).Amount : null);
            })
            .ToList();
    }

    // ── My time (spec §7) ────────────────────────────────────────────────────

    private async Task<MyTimeSummary> BuildMyTimeAsync(DateOnly today, CancellationToken ct)
    {
        var dow = (int)today.DayOfWeek;
        var monday = today.AddDays(dow == 0 ? -6 : 1 - dow);
        var weekDays = await timesheets.GetDaysInRangeAsync(currentUser.UserId, monday, monday.AddDays(6), ct);
        var weekHours = weekDays.Sum(d => d.WorkedHours);

        var ownTerms = await terms.GetLatestAsync(currentUser.UserId, ct);
        decimal? target = ownTerms is null ? null : ownTerms.ExpectedWeeklyHours ?? ownTerms.WeeklyHours;

        var prev = today.AddMonths(-1);
        var prevSheet = await timesheets.GetForUserAsync(currentUser.UserId, prev.Year, prev.Month, ct);
        var prevStatus = prevSheet switch
        {
            null => "NoData",
            { Status: TimesheetStatus.Draft } => "NotSubmitted",
            _ => prevSheet.Status.ToString(),
        };
        var prevName = new DateOnly(prev.Year, prev.Month, 1).ToString("MMMM", CultureInfo.GetCultureInfo("en"));

        var entitlement = await absences.GetEntitlementAsync(currentUser.UserId, today.Year, ct);

        return new MyTimeSummary(
            WeekHours: weekHours,
            WeekTargetHours: target,
            PreviousMonthName: prevName,
            PreviousMonthStatus: prevStatus,
            VacationLeftDays: entitlement?.RemainingDays,
            HasContract: ownTerms is not null);
    }
}
