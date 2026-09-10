using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Domain.WorkTimeManagement.Services;

namespace Proposly.Domain.Tests.WorkTimeManagement;

public class ComplianceEvaluatorTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid TimesheetId = Guid.NewGuid();
    private static readonly DateOnly Jan1 = new(2026, 1, 1);

    private static WorkTimePolicy Austria() => WorkTimePolicyDefaults.For("AT", CompanyId, Jan1)!;
    private static WorkTimePolicy Germany() => WorkTimePolicyDefaults.For("DE", CompanyId, Jan1)!;

    private static WorkDayEntry Day(
        string date, string start, string end, int breakMinutes, bool crossesMidnight = false)
        => WorkDayEntry.Create(
            TimesheetId, DateOnly.Parse(date), TimeOnly.Parse(start), TimeOnly.Parse(end),
            breakMinutes, crossesMidnight);

    private static IReadOnlyList<TimesheetBreach> Evaluate(
        WorkTimePolicy policy,
        IReadOnlyCollection<WorkDayEntry> days,
        IReadOnlyCollection<WorkDayEntry>? preceding = null)
        => ComplianceEvaluator.Evaluate(TimesheetId, days, policy, preceding);

    // --- The load-bearing guarantee ---

    [Fact]
    public void A_day_breaching_every_limit_is_reported_and_never_blocked()
    {
        // 15.25 hours with a 15-minute break exceeds the Austrian 12h daily maximum and falls
        // short of the 30-minute break minimum. Both must be reported, and the entry itself must
        // have been accepted — WorkDayEntry.Create did not throw to get here.
        var day = Day("2026-03-11", "06:00", "21:30", 15);

        var breaches = Evaluate(Austria(), [day]);

        Assert.Contains(breaches, b => b.Kind == BreachKind.DailyMaximum);
        Assert.Contains(breaches, b => b.Kind == BreachKind.InsufficientBreak);
        Assert.Equal(15.25m, day.WorkedHours);
    }

    [Fact]
    public void A_fully_compliant_month_produces_no_breaches()
    {
        var days = new[]
        {
            Day("2026-03-02", "08:00", "16:30", 30),
            Day("2026-03-03", "08:00", "16:30", 30),
            Day("2026-03-04", "08:00", "16:30", 30),
            Day("2026-03-05", "08:00", "16:30", 30),
            Day("2026-03-06", "08:00", "16:30", 30),
        };

        Assert.Empty(Evaluate(Austria(), days));
    }

    [Fact]
    public void An_empty_month_produces_no_breaches()
    {
        Assert.Empty(Evaluate(Austria(), []));
    }

    // --- Daily maximum ---

    [Fact]
    public void Exceeding_the_daily_maximum_is_flagged_with_the_limit_and_the_actual()
    {
        var breaches = Evaluate(Austria(), [Day("2026-03-11", "06:00", "19:30", 30)]);

        var breach = Assert.Single(breaches, b => b.Kind == BreachKind.DailyMaximum);
        Assert.Equal(12m, breach.LimitValue);
        Assert.Equal(13m, breach.ActualValue);
        Assert.Equal(new DateOnly(2026, 3, 11), breach.Date);
    }

    [Fact]
    public void A_day_exactly_at_the_daily_maximum_is_not_flagged()
    {
        // 12h worked exactly — at the limit, not over it.
        Assert.DoesNotContain(
            Evaluate(Austria(), [Day("2026-03-11", "06:00", "18:30", 30)]),
            b => b.Kind == BreachKind.DailyMaximum);
    }

    [Fact]
    public void The_german_daily_maximum_is_stricter_than_the_austrian_one()
    {
        var day = Day("2026-03-11", "07:00", "18:30", 45); // 10.75h

        Assert.Contains(Evaluate(Germany(), [day]), b => b.Kind == BreachKind.DailyMaximum);
        Assert.DoesNotContain(Evaluate(Austria(), [day]), b => b.Kind == BreachKind.DailyMaximum);
    }

    // --- Breaks, including tier selection ---

    [Fact]
    public void A_short_break_above_six_hours_is_flagged_against_the_thirty_minute_tier()
    {
        var breaches = Evaluate(Austria(), [Day("2026-03-03", "08:00", "15:20", 20)]); // 7h

        var breach = Assert.Single(breaches, b => b.Kind == BreachKind.InsufficientBreak);
        Assert.Equal(30m, breach.LimitValue);
        Assert.Equal(20m, breach.ActualValue);
    }

    [Fact]
    public void A_day_under_six_hours_needs_no_break()
    {
        Assert.Empty(Evaluate(Austria(), [Day("2026-03-03", "08:00", "13:00", 0)])); // 5h
    }

    [Fact]
    public void Germany_applies_the_forty_five_minute_tier_above_nine_hours()
    {
        // 9.5h worked with 30 minutes: fine under the 6h tier, short under the 9h tier.
        var breaches = Evaluate(Germany(), [Day("2026-03-04", "08:00", "18:00", 30)]);

        var breach = Assert.Single(breaches, b => b.Kind == BreachKind.InsufficientBreak);
        Assert.Equal(45m, breach.LimitValue);
    }

    [Fact]
    public void Austria_has_no_second_tier_so_the_same_day_passes_its_break_rule()
    {
        Assert.DoesNotContain(
            Evaluate(Austria(), [Day("2026-03-04", "08:00", "18:00", 30)]),
            b => b.Kind == BreachKind.InsufficientBreak);
    }

    // --- Daily rest ---

    [Fact]
    public void Too_little_rest_between_consecutive_days_is_flagged_on_the_later_day()
    {
        var days = new[]
        {
            Day("2026-03-03", "13:00", "22:00", 30),
            Day("2026-03-04", "06:00", "14:00", 30), // only 8h rest
        };

        var breach = Assert.Single(
            Evaluate(Austria(), days), b => b.Kind == BreachKind.InsufficientDailyRest);

        Assert.Equal(new DateOnly(2026, 3, 4), breach.Date);
        Assert.Equal(11m, breach.LimitValue);
        Assert.Equal(8m, breach.ActualValue);
    }

    [Fact]
    public void Eleven_hours_of_rest_exactly_is_not_flagged()
    {
        var days = new[]
        {
            Day("2026-03-03", "12:00", "20:00", 30),
            Day("2026-03-04", "07:00", "15:00", 30), // exactly 11h
        };

        Assert.DoesNotContain(
            Evaluate(Austria(), days), b => b.Kind == BreachKind.InsufficientDailyRest);
    }

    [Fact]
    public void A_rest_breach_spanning_the_month_boundary_is_found_via_the_preceding_days()
    {
        var preceding = new[] { Day("2026-02-28", "14:00", "23:00", 30) };
        var days = new[] { Day("2026-03-01", "06:00", "14:00", 30) }; // 7h rest

        var breach = Assert.Single(
            Evaluate(Austria(), days, preceding), b => b.Kind == BreachKind.InsufficientDailyRest);

        Assert.Equal(new DateOnly(2026, 3, 1), breach.Date);
    }

    [Fact]
    public void Without_preceding_days_no_rest_breach_is_invented_for_the_first_day()
    {
        Assert.DoesNotContain(
            Evaluate(Austria(), [Day("2026-03-01", "06:00", "14:00", 30)]),
            b => b.Kind == BreachKind.InsufficientDailyRest);
    }

    [Fact]
    public void A_night_shift_is_measured_from_its_real_end_on_the_following_day()
    {
        var days = new[]
        {
            Day("2026-03-03", "22:00", "06:00", 30, crossesMidnight: true), // ends 04-03 06:00
            Day("2026-03-04", "12:00", "20:00", 30),                        // 6h rest
        };

        var breach = Assert.Single(
            Evaluate(Austria(), days), b => b.Kind == BreachKind.InsufficientDailyRest);

        Assert.Equal(6m, breach.ActualValue);
    }

    // --- Weekly maximum ---

    [Fact]
    public void Exceeding_the_weekly_maximum_is_flagged_against_the_week_start()
    {
        // Mon 2 Mar to Sat 7 Mar 2026, 11h worked each day = 66h > 60h.
        var days = Enumerable.Range(2, 6)
            .Select(d => Day($"2026-03-{d:00}", "06:00", "17:30", 30))
            .ToArray();

        var breach = Assert.Single(
            Evaluate(Austria(), days), b => b.Kind == BreachKind.WeeklyMaximum);

        Assert.Equal(new DateOnly(2026, 3, 2), breach.WeekStartDate); // the Monday
        Assert.Equal(60m, breach.LimitValue);
        Assert.Equal(66m, breach.ActualValue);
        Assert.Null(breach.Date);
    }

    [Fact]
    public void A_week_straddling_the_month_boundary_counts_the_preceding_days_too()
    {
        // Week of Mon 23 Feb 2026. Three days in February, three in March, 11h each = 66h.
        var preceding = Enumerable.Range(23, 3)
            .Select(d => Day($"2026-02-{d:00}", "06:00", "17:30", 30))
            .ToArray();

        var days = Enumerable.Range(26, 3)
            .Select(d => Day($"2026-02-{d:00}", "06:00", "17:30", 30))
            .ToArray();

        var breaches = Evaluate(Austria(), days, preceding);

        var weekly = Assert.Single(breaches, b => b.Kind == BreachKind.WeeklyMaximum);
        Assert.Equal(66m, weekly.ActualValue);
    }

    // --- Averaging window ---

    [Fact]
    public void A_sustained_overload_breaches_the_averaging_window()
    {
        // 17 weeks at 60h each averages 60h > the 48h Austrian cap.
        var start = new DateOnly(2025, 11, 3); // a Monday
        var all = new List<WorkDayEntry>();
        for (var week = 0; week < 17; week++)
            for (var day = 0; day < 5; day++)
                all.Add(Day(start.AddDays(week * 7 + day).ToString("yyyy-MM-dd"), "06:00", "18:30", 30));

        var lastWeek = all.Where(d => d.Date >= start.AddDays(16 * 7)).ToList();
        var preceding = all.Where(d => d.Date < start.AddDays(16 * 7)).ToList();

        Assert.Contains(
            Evaluate(Austria(), lastWeek, preceding), b => b.Kind == BreachKind.AveragingWindow);
    }

    // --- Approved absence excuses the day entirely (FR-057) ---

    private static AbsenceRequest ApprovedLeave(string start, string end)
    {
        var request = AbsenceRequest.Create(
            CompanyId, Guid.NewGuid(), AbsenceType.SickLeave,
            DateOnly.Parse(start), DateOnly.Parse(end), false, false, 1m);

        request.Approve(Guid.NewGuid());
        return request;
    }

    [Fact]
    public void A_day_covered_by_approved_leave_contributes_nothing_to_any_limit()
    {
        // 13.5 hours with a 15-minute break would breach both the daily maximum and the break
        // minimum — but the employee was on approved sick leave that day, so the hours are not
        // judged as working time.
        var day = Day("2026-03-11", "06:00", "19:45", 15);

        Assert.NotEmpty(Evaluate(Austria(), [day]));

        var withLeave = ComplianceEvaluator.Evaluate(
            TimesheetId, [day], Austria(), null, [ApprovedLeave("2026-03-11", "2026-03-11")]);

        Assert.Empty(withLeave);
    }

    [Fact]
    public void Leave_on_one_day_does_not_excuse_a_breach_on_another()
    {
        var days = new[]
        {
            Day("2026-03-10", "06:00", "19:45", 15), // breaches
            Day("2026-03-11", "06:00", "19:45", 15), // breaches, but covered by leave
        };

        var breaches = ComplianceEvaluator.Evaluate(
            TimesheetId, days, Austria(), null, [ApprovedLeave("2026-03-11", "2026-03-11")]);

        Assert.All(breaches, b => Assert.NotEqual(new DateOnly(2026, 3, 11), b.Date));
        Assert.Contains(breaches, b => b.Date == new DateOnly(2026, 3, 10));
    }

    [Fact]
    public void A_pending_absence_does_not_excuse_anything()
    {
        // Only an approved absence excuses a day; a request still awaiting a decision does not.
        var pending = AbsenceRequest.Create(
            CompanyId, Guid.NewGuid(), AbsenceType.Vacation,
            new DateOnly(2026, 3, 11), new DateOnly(2026, 3, 11), false, false, 1m);

        var breaches = ComplianceEvaluator.Evaluate(
            TimesheetId, [Day("2026-03-11", "06:00", "19:45", 15)], Austria(), null, [pending]);

        Assert.NotEmpty(breaches);
    }

    [Fact]
    public void Leave_covering_a_preceding_day_removes_it_from_the_rest_check()
    {
        var preceding = new[] { Day("2026-02-28", "14:00", "23:00", 30) };
        var days = new[] { Day("2026-03-01", "06:00", "14:00", 30) }; // 7h rest

        var breaches = ComplianceEvaluator.Evaluate(
            TimesheetId, days, Austria(), preceding, [ApprovedLeave("2026-02-28", "2026-02-28")]);

        Assert.DoesNotContain(breaches, b => b.Kind == BreachKind.InsufficientDailyRest);
    }

    [Fact]
    public void One_heavy_week_alone_does_not_breach_the_averaging_window()
    {
        // 55h in a single week averages ~3.2h across the 17-week window.
        var days = Enumerable.Range(2, 5)
            .Select(d => Day($"2026-03-{d:00}", "06:00", "17:30", 30))
            .ToArray();

        Assert.DoesNotContain(
            Evaluate(Austria(), days), b => b.Kind == BreachKind.AveragingWindow);
    }
}
