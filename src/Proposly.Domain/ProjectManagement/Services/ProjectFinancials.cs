using Proposly.Domain.ProjectManagement.Entities;
using Proposly.Domain.ProjectManagement.Enums;

namespace Proposly.Domain.ProjectManagement.Services;

/// <summary>
/// Single source of truth for project money math (redesign spec §3). Pure — takes the loaded
/// aggregate, returns plain decimals in the project currency. Snapshot rates on time entries are
/// used for everything "actual", so member rate changes never rewrite history.
/// </summary>
public static class ProjectFinancials
{
    public static ProjectFinancialsResult Calculate(Project project)
    {
        var members = project.Members;
        var entries = project.TimeEntries;
        var tasks = project.Tasks;

        var plannedLabor = members.Sum(m => m.AllocatedHours * m.CostRate.Amount);
        var plannedHours = members.Sum(m => m.AllocatedHours);
        var loggedHours = entries.Sum(e => e.HoursWorked);
        var actualLabor = entries.Sum(e => e.HoursWorked * e.CostRateSnapshot.Amount);
        var actualExpenses = project.Expenses.Sum(e => e.Amount.Amount);
        var actualCost = actualLabor + actualExpenses;

        // Overrun: tasks already logged past their estimate push the forecast up.
        var avgCostRate = members.Count > 0 ? members.Average(m => m.CostRate.Amount) : 0m;
        var overrun = 0m;
        foreach (var task in tasks)
        {
            if (!task.EstimatedHours.HasValue) continue;
            var loggedOnTask = entries.Where(e => e.TaskId == task.Id).Sum(e => e.HoursWorked);
            var over = loggedOnTask - task.EstimatedHours.Value;
            if (over <= 0) continue;
            var rate = task.AssignedMemberId.HasValue
                ? members.FirstOrDefault(m => m.Id == task.AssignedMemberId.Value)?.CostRate.Amount ?? avgCostRate
                : avgCostRate;
            overrun += over * rate;
        }

        var expenseBudget = project.ExpenseBudget?.Amount ?? 0m;
        var forecastLabor = actualLabor + Math.Max(plannedLabor - actualLabor, 0m) + overrun;
        var forecastExpenses = Math.Max(expenseBudget, actualExpenses);
        var forecastCost = forecastLabor + forecastExpenses;

        var billingType = project.BillingType;
        decimal? revenue = billingType switch
        {
            BillingType.FixedPrice => project.Budget.Amount,
            BillingType.TimeAndMaterials => members.Sum(m => m.AllocatedHours * m.BillRate.Amount),
            _ => null, // Internal: no client revenue
        };
        var revenueToDate = billingType == BillingType.TimeAndMaterials
            ? entries.Sum(e => e.HoursWorked * e.BillRateSnapshot.Amount)
            : 0m;

        // Nothing logged yet → "Projected": planned labor + actual expenses, no forecast noise.
        // Once hours exist → "Forecast": full forecast cost including the expense budget.
        var isProjected = loggedHours == 0;
        var resultCost = isProjected ? plannedLabor + actualExpenses : forecastCost;

        decimal result;
        decimal? marginPercent;
        if (billingType == BillingType.Internal)
        {
            result = project.Budget.Amount - resultCost; // budget headroom
            marginPercent = null;
        }
        else
        {
            result = (revenue ?? 0m) - resultCost;
            marginPercent = revenue is > 0 ? Math.Round(result / revenue.Value * 100m, 1) : null;
        }

        return new ProjectFinancialsResult(
            PlannedLaborCost: plannedLabor,
            PlannedHours: plannedHours,
            LoggedHours: loggedHours,
            ActualLaborCost: actualLabor,
            ActualExpenses: actualExpenses,
            ActualCost: actualCost,
            ForecastLaborCost: forecastLabor,
            ForecastExpenses: forecastExpenses,
            ForecastCost: forecastCost,
            Revenue: revenue,
            RevenueToDate: revenueToDate,
            Result: result,
            MarginPercent: marginPercent,
            IsProjected: isProjected,
            Currency: project.Budget.Currency);
    }
}

/// <summary>All money values in the project currency. <see cref="Result"/> is profit, or budget
/// headroom for internal projects. <see cref="IsProjected"/> = no hours logged yet.</summary>
public sealed record ProjectFinancialsResult(
    decimal PlannedLaborCost,
    decimal PlannedHours,
    decimal LoggedHours,
    decimal ActualLaborCost,
    decimal ActualExpenses,
    decimal ActualCost,
    decimal ForecastLaborCost,
    decimal ForecastExpenses,
    decimal ForecastCost,
    decimal? Revenue,
    decimal RevenueToDate,
    decimal Result,
    decimal? MarginPercent,
    bool IsProjected,
    string Currency);
