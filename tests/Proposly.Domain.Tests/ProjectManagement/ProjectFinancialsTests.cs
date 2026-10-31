using Proposly.Domain.ProjectManagement.Entities;
using Proposly.Domain.ProjectManagement.Enums;
using Proposly.Domain.ProjectManagement.Services;
using Proposly.Shared.ValueObjects;

namespace Proposly.Domain.Tests.ProjectManagement;

/// <summary>
/// The worked example from the redesign handoff (PHASES.md): Fixed price, budget 150.000,
/// expense budget 2.000; members A 70/110/320 h · B 50/85/80 h · C 50/85/320 h.
/// </summary>
public class ProjectFinancialsTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();

    private static Project NewProject()
    {
        var project = Project.Create(
            CompanyId, "Redesign", null, Guid.NewGuid(), "Client",
            new Money(150_000m, "EUR"),
            new DateOnly(2026, 10, 31), new DateOnly(2026, 12, 31));
        project.SetExpenseBudget(new Money(2_000m, "EUR"));
        return project;
    }

    private static (Project p, ProjectMember a, ProjectMember b, ProjectMember c) WithTeam()
    {
        var p = NewProject();
        var a = p.AddMember(Guid.NewGuid(), "A", "Developer", new Money(70m, "EUR"));
        var b = p.AddMember(Guid.NewGuid(), "B", "PM", new Money(50m, "EUR"));
        var c = p.AddMember(Guid.NewGuid(), "C", "Developer", new Money(50m, "EUR"));
        p.UpdateMemberEngagement(a.Id, "Developer", new Money(70m, "EUR"), new Money(110m, "EUR"), 320m);
        p.UpdateMemberEngagement(b.Id, "PM", new Money(50m, "EUR"), new Money(85m, "EUR"), 80m);
        p.UpdateMemberEngagement(c.Id, "Developer", new Money(50m, "EUR"), new Money(85m, "EUR"), 320m);
        return (p, a, b, c);
    }

    [Fact]
    public void Planned_labor_and_hours_follow_allocation_times_cost_rate()
    {
        var (p, _, _, _) = WithTeam();

        var fin = ProjectFinancials.Calculate(p);

        Assert.Equal(42_400m, fin.PlannedLaborCost);
        Assert.Equal(720m, fin.PlannedHours);
        Assert.True(fin.IsProjected);
    }

    [Fact]
    public void Fixed_price_projected_profit_is_budget_minus_planned_labor()
    {
        var (p, _, _, _) = WithTeam();

        var fin = ProjectFinancials.Calculate(p);

        Assert.Equal(150_000m, fin.Revenue);
        Assert.Equal(107_600m, fin.Result);
        Assert.Equal(71.7m, fin.MarginPercent);
    }

    [Fact]
    public void Time_and_materials_projects_revenue_from_bill_rates()
    {
        var (p, _, _, _) = WithTeam();
        p.SetBillingType(BillingType.TimeAndMaterials);

        var fin = ProjectFinancials.Calculate(p);

        Assert.Equal(69_200m, fin.Revenue);
        Assert.Equal(26_800m, fin.Result);
        Assert.Equal(38.7m, fin.MarginPercent);
    }

    [Fact]
    public void Internal_projects_report_budget_headroom_without_margin()
    {
        var (p, _, _, _) = WithTeam();
        p.SetBillingType(BillingType.Internal);

        var fin = ProjectFinancials.Calculate(p);

        Assert.Null(fin.Revenue);
        Assert.Equal(107_600m, fin.Result); // 150.000 − 42.400 planned
        Assert.Null(fin.MarginPercent);
    }

    [Fact]
    public void Actual_cost_uses_snapshot_rates_plus_expenses()
    {
        var (p, a, b, c) = WithTeam();
        var date = new DateOnly(2026, 11, 5);
        p.LogTime(a.Id, 22m, null, date);
        p.LogTime(c.Id, 7m, null, date);
        p.LogTime(b.Id, 1.5m, null, date);
        p.AddExpense("CI runners", new Money(206.40m, "EUR"), ExpenseCategory.Other, date);

        var fin = ProjectFinancials.Calculate(p);

        Assert.Equal(1_965m, fin.ActualLaborCost);
        Assert.Equal(2_171.40m, fin.ActualCost);
        Assert.False(fin.IsProjected);
    }

    [Fact]
    public void Rate_change_after_logging_does_not_rewrite_history()
    {
        var (p, a, _, _) = WithTeam();
        p.LogTime(a.Id, 10m, null, new DateOnly(2026, 11, 5));

        p.UpdateMemberEngagement(a.Id, "Developer", new Money(100m, "EUR"), new Money(150m, "EUR"), 320m);
        var fin = ProjectFinancials.Calculate(p);

        Assert.Equal(700m, fin.ActualLaborCost); // still 10 h × 70
    }

    [Fact]
    public void Forecast_adds_task_overrun_and_expense_budget()
    {
        var (p, a, b, c) = WithTeam();
        var date = new DateOnly(2026, 11, 5);
        var task = p.AddTask("Integration", null, 24m, date, date.AddDays(10), assignedMemberId: a.Id);
        p.LogTime(a.Id, 25m, null, date, task.Id); // 1 h over the 24 h estimate, A costs 70
        p.LogTime(c.Id, 7m, null, date);
        p.LogTime(b.Id, 1.5m, null, date);
        p.AddExpense("CI runners", new Money(206.40m, "EUR"), ExpenseCategory.Other, date);

        var fin = ProjectFinancials.Calculate(p);

        Assert.Equal(42_470m, fin.ForecastLaborCost); // PL + 1 h × 70 overrun
        Assert.Equal(2_000m, fin.ForecastExpenses);   // expense budget > actuals
        Assert.Equal(44_470m, fin.ForecastCost);
        Assert.Equal(105_530m, fin.Result);           // 150.000 − 44.470
        Assert.Equal(70.4m, fin.MarginPercent);
    }

    [Fact]
    public void New_members_start_with_cost_and_bill_equal_to_the_legacy_rate()
    {
        var p = NewProject();
        var m = p.AddMember(Guid.NewGuid(), "A", "Dev", new Money(80m, "EUR"));

        Assert.Equal(80m, m.CostRate.Amount);
        Assert.Equal(80m, m.BillRate.Amount);
        Assert.Equal(0m, m.AllocatedHours);
    }

    [Fact]
    public void Time_entries_snapshot_both_cost_and_bill_rates()
    {
        var (p, a, _, _) = WithTeam();
        var entry = p.LogTime(a.Id, 5m, null, new DateOnly(2026, 11, 5));

        Assert.Equal(70m, entry.CostRateSnapshot.Amount);
        Assert.Equal(110m, entry.BillRateSnapshot.Amount);
        Assert.Equal(70m, entry.HourlyRateSnapshot.Amount); // legacy column follows cost
    }
}
