using Proposly.Application.Abstractions;
using Proposly.Application.ProjectManagement.Responses;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Domain.ProjectManagement.Services;

namespace Proposly.Application.ProjectManagement.Queries.GetProjectOverview;

public sealed class GetProjectOverviewQueryHandler : IQueryHandler<GetProjectOverviewQuery, ProjectOverviewResponse?>
{
    private readonly IProjectRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public GetProjectOverviewQueryHandler(IProjectRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<ProjectOverviewResponse?> HandleAsync(GetProjectOverviewQuery query, CancellationToken cancellationToken = default)
    {
        var project = await _repository.GetByIdAsync(query.ProjectId, cancellationToken);
        if (project is null) return null;

        // Owner/Admin see money; Members see hours only — money fields stay null for them,
        // so rates and profit never leave the API (spec §6).
        var canViewMoney = _currentUser.CanViewAllEmployees;
        var fin = ProjectFinancials.Calculate(project);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // KPI strip
        int? daysLeft = null, dayOfTotal = null, totalDays = null;
        if (project.Deadline is { } deadline)
        {
            totalDays = Math.Max(deadline.DayNumber - project.StartDate.DayNumber + 1, 1);
            daysLeft = Math.Max(deadline.DayNumber - today.DayNumber, 0);
            dayOfTotal = Math.Clamp(today.DayNumber - project.StartDate.DayNumber + 1, 0, totalDays.Value);
        }

        var tasks = project.Tasks;
        var openTasks = tasks.Where(t => t.Status != Domain.ProjectManagement.Enums.ProjectTaskStatus.Done).ToList();
        var tasksWithoutDates = openTasks.Count(t => t.StartDate is null || t.DueDate is null);

        var nextMilestone = project.Milestones
            .Where(m => !m.IsCompleted)
            .OrderBy(m => m.DueDate)
            .FirstOrDefault();

        int LinkedTasks(Guid milestoneId) => tasks.Count(t => t.MilestoneId == milestoneId);
        int DoneLinkedTasks(Guid milestoneId) =>
            tasks.Count(t => t.MilestoneId == milestoneId && t.Status == Domain.ProjectManagement.Enums.ProjectTaskStatus.Done);

        var kpis = new OverviewKpis(
            StartDate: project.StartDate,
            Deadline: project.Deadline,
            DaysLeft: daysLeft,
            DayOfTotal: dayOfTotal,
            TotalDays: totalDays,
            BudgetAmount: project.Budget.Amount,
            SpentAmount: canViewMoney ? fin.ActualCost : null,
            TasksDone: tasks.Count - openTasks.Count,
            TasksTotal: tasks.Count,
            TasksWithoutDates: tasksWithoutDates,
            NextMilestoneTitle: nextMilestone?.Title,
            NextMilestoneDue: nextMilestone?.DueDate,
            NextMilestoneLinkedTasks: nextMilestone is null ? 0 : LinkedTasks(nextMilestone.Id));

        var checklist = new OverviewChecklist(
            MembersCount: project.Members.Count,
            AllRatesSet: project.Members.Count > 0 && project.Members.All(m => m.CostRate.Amount > 0),
            MilestonesCount: project.Milestones.Count,
            OpenTasks: openTasks.Count,
            ScheduledOpenTasks: openTasks.Count(t => t.StartDate is not null && t.DueDate is not null),
            EstimatedOpenTasks: openTasks.Count(t => t.EstimatedHours is not null),
            TimeEntriesCount: project.TimeEntries.Count);

        var profitability = canViewMoney
            ? new OverviewProfitability(
                Revenue: fin.Revenue,
                RevenueToDate: fin.RevenueToDate,
                PlannedLaborCost: fin.PlannedLaborCost,
                PlannedHours: fin.PlannedHours,
                LoggedHours: fin.LoggedHours,
                ActualLaborCost: fin.ActualLaborCost,
                ActualExpenses: fin.ActualExpenses,
                ActualCost: fin.ActualCost,
                ForecastCost: fin.ForecastCost,
                Result: fin.Result,
                MarginPercent: fin.MarginPercent,
                IsProjected: fin.IsProjected)
            : null;

        var milestones = project.Milestones
            .OrderBy(m => m.IsCompleted)
            .ThenBy(m => m.DueDate)
            .Take(4)
            .Select(m => new OverviewMilestone(m.Id, m.Title, m.DueDate, m.IsCompleted, LinkedTasks(m.Id), DoneLinkedTasks(m.Id)))
            .ToList();

        var team = project.Members
            .OrderBy(m => m.Name)
            .Select(m => new OverviewTeamMember(
                m.Id,
                m.UserId,
                m.Name,
                m.Role,
                canViewMoney ? m.CostRate.Amount : null,
                canViewMoney ? m.BillRate.Amount : null,
                m.AllocatedHours))
            .ToList();

        return new ProjectOverviewResponse(
            BillingType: project.BillingType.ToString(),
            Currency: project.Budget.Currency,
            CanViewMoney: canViewMoney,
            Kpis: kpis,
            Checklist: checklist,
            Profitability: profitability,
            Milestones: milestones,
            Team: team,
            PlannedHours: fin.PlannedHours);
    }
}
