using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Domain.WorkTimeManagement.Services;

namespace Proposly.Domain.Tests.WorkTimeManagement;

/// <summary>
/// Target hours and chargeable days once employment terms and the company calendar exist.
/// March 2026 has 22 weekdays and no weekend-spanning oddities, which makes it a clean fixture.
/// </summary>
public class TargetHoursTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private static EmploymentTerms Terms(
        string validFrom = "2026-01-01",
        decimal weeklyHours = 38.5m,
        WeekDays days = WeekDays.MondayToFriday)
        => EmploymentTerms.Create(
            CompanyId, UserId, DateOnly.Parse(validFrom), weeklyHours, days, 25m);

    private static NonWorkingDay Holiday(string date, string name = "Holiday")
        => NonWorkingDay.Create(CompanyId, DateOnly.Parse(date), name, NonWorkingDayKind.PublicHoliday);

    private static NonWorkingDay Closure(string date, bool consumesVacation = true)
        => NonWorkingDay.Create(
            CompanyId, DateOnly.Parse(date), "Betriebsurlaub",
            NonWorkingDayKind.CompanyClosure, consumesVacation);

    private static AbsenceRequest ApprovedLeave(
        string start, string end, bool firstHalf = false, bool lastHalf = false)
    {
        var request = AbsenceRequest.Create(
            CompanyId, UserId, AbsenceType.Vacation,
            DateOnly.Parse(start), DateOnly.Parse(end), firstHalf, lastHalf, 1m);

        request.Approve(Guid.NewGuid());
        return request;
    }

    // --- Expected working days ---

    [Fact]
    public void March_2026_has_twenty_two_working_days_on_a_monday_to_friday_pattern()
    {
        Assert.Equal(22, WorkingDayCalculator.ExpectedWorkingDays(2026, 3, [Terms()]));
    }

    [Fact]
    public void A_public_holiday_on_a_working_weekday_removes_a_day()
    {
        // 2 March 2026 is a Monday.
        Assert.Equal(21, WorkingDayCalculator.ExpectedWorkingDays(
            2026, 3, [Terms()], [Holiday("2026-03-02")]));
    }

    [Fact]
    public void A_holiday_on_a_non_working_weekday_changes_nothing()
    {
        // 7 March 2026 is a Saturday, which this employee does not work anyway.
        Assert.Equal(22, WorkingDayCalculator.ExpectedWorkingDays(
            2026, 3, [Terms()], [Holiday("2026-03-07")]));
    }

    [Fact]
    public void A_company_closure_stays_a_working_day_because_the_employee_must_cover_it()
    {
        // The employee spends entitlement on it, so it still counts toward what they owe.
        Assert.Equal(22, WorkingDayCalculator.ExpectedWorkingDays(
            2026, 3, [Terms()], [Closure("2026-03-02")]));
    }

    [Fact]
    public void A_closure_granted_outright_does_remove_a_day()
    {
        Assert.Equal(21, WorkingDayCalculator.ExpectedWorkingDays(
            2026, 3, [Terms()], [Closure("2026-03-02", consumesVacation: false)]));
    }

    [Fact]
    public void A_four_day_week_has_fewer_working_days()
    {
        var fourDay = WeekDays.Monday | WeekDays.Tuesday | WeekDays.Wednesday | WeekDays.Thursday;

        Assert.Equal(realWorkingDays(), WorkingDayCalculator.ExpectedWorkingDays(
            2026, 3, [Terms(days: fourDay)]));

        static int realWorkingDays()
        {
            // March 2026: Mondays to Thursdays only.
            var count = 0;
            for (var d = new DateOnly(2026, 3, 1); d.Month == 3; d = d.AddDays(1))
                if (d.DayOfWeek is >= DayOfWeek.Monday and <= DayOfWeek.Thursday) count++;
            return count;
        }
    }

    // --- Target hours ---

    [Fact]
    public void Target_hours_are_working_days_times_daily_hours()
    {
        // 22 days at 7.7 hours = 169.4
        Assert.Equal(169.4m, WorkingDayCalculator.TargetHoursForMonth(2026, 3, [Terms()]));
    }

    [Fact]
    public void A_holiday_reduces_the_target_by_one_day()
    {
        // 21 days at 7.7 = 161.7
        Assert.Equal(161.7m, WorkingDayCalculator.TargetHoursForMonth(
            2026, 3, [Terms()], [Holiday("2026-03-02")]));
    }

    [Fact]
    public void Approved_absence_reduces_the_target_rather_than_counting_as_hours_worked()
    {
        // Two days of leave off 22: 20 at 7.7 = 154.
        var leave = ApprovedLeave("2026-03-03", "2026-03-04");

        Assert.Equal(154m, WorkingDayCalculator.TargetHoursForMonth(
            2026, 3, [Terms()], null, [leave]));
    }

    [Fact]
    public void A_half_day_of_leave_reduces_the_target_by_half_a_day()
    {
        // 22 days less half a day = 21.5 at 7.7 = 165.55
        var leave = ApprovedLeave("2026-03-03", "2026-03-03", firstHalf: true);

        Assert.Equal(165.55m, WorkingDayCalculator.TargetHoursForMonth(
            2026, 3, [Terms()], null, [leave]));
    }

    [Fact]
    public void Leave_falling_on_a_holiday_is_not_deducted_twice()
    {
        // The holiday already removed the day; the leave covering it must not remove it again.
        var leave = ApprovedLeave("2026-03-02", "2026-03-02");

        Assert.Equal(161.7m, WorkingDayCalculator.TargetHoursForMonth(
            2026, 3, [Terms()], [Holiday("2026-03-02")], [leave]));
    }

    [Fact]
    public void Overlapping_approved_absences_never_deduct_more_than_a_whole_day()
    {
        var first = ApprovedLeave("2026-03-03", "2026-03-03");
        var second = ApprovedLeave("2026-03-03", "2026-03-03");

        // 21 days at 7.7 = 161.7, not 20 days.
        Assert.Equal(161.7m, WorkingDayCalculator.TargetHoursForMonth(
            2026, 3, [Terms()], null, [first, second]));
    }

    [Fact]
    public void A_pending_absence_does_not_reduce_the_target()
    {
        var pending = AbsenceRequest.Create(
            CompanyId, UserId, AbsenceType.Vacation,
            new DateOnly(2026, 3, 3), new DateOnly(2026, 3, 4), false, false, 2m);

        Assert.Equal(169.4m, WorkingDayCalculator.TargetHoursForMonth(
            2026, 3, [Terms()], null, [pending]));
    }

    // --- Missing and changing terms ---

    [Fact]
    public void With_no_employment_terms_the_target_is_unavailable_rather_than_zero()
    {
        // Zero would read as a full month of deficit; null says "we do not know".
        Assert.Null(WorkingDayCalculator.TargetHoursForMonth(2026, 3, []));
    }

    [Fact]
    public void Terms_starting_after_the_month_do_not_apply_to_it()
    {
        Assert.Null(WorkingDayCalculator.TargetHoursForMonth(2026, 3, [Terms("2026-07-01")]));
    }

    [Fact]
    public void A_contract_change_mid_month_is_resolved_per_day()
    {
        // 38.5h Mon-Fri until 15 March, then 30h Mon-Fri. Terms are picked per day, so the month
        // is a genuine blend rather than whichever version happened to start it.
        var first = Terms("2026-01-01", weeklyHours: 38.5m);
        var second = Terms("2026-03-16", weeklyHours: 30m);
        first.CloseAt(second.ValidFrom);

        var blended = WorkingDayCalculator.TargetHoursForMonth(2026, 3, [first, second])!.Value;
        var allOld = WorkingDayCalculator.TargetHoursForMonth(2026, 3, [Terms(weeklyHours: 38.5m)])!.Value;
        var allNew = WorkingDayCalculator.TargetHoursForMonth(2026, 3, [Terms(weeklyHours: 30m)])!.Value;

        Assert.True(blended < allOld);
        Assert.True(blended > allNew);
    }

    [Fact]
    public void Days_before_the_terms_start_are_excluded_from_a_partial_month()
    {
        // Someone joining on 16 March owes only the rest of the month.
        var joined = Terms("2026-03-16");

        var partial = WorkingDayCalculator.TargetHoursForMonth(2026, 3, [joined])!.Value;
        var full = WorkingDayCalculator.TargetHoursForMonth(2026, 3, [Terms()])!.Value;

        Assert.True(partial < full);
        Assert.True(partial > 0m);
    }

    // --- Absence day counting with the calendar ---

    [Fact]
    public void A_holiday_inside_a_vacation_range_is_not_charged()
    {
        // Mon 2 to Fri 6 March, with the Monday a holiday: four days, not five.
        var consumed = WorkingDayCalculator.ConsumedDays(
            new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 6), false, false,
            WeekDays.MondayToFriday, [Holiday("2026-03-02")]);

        Assert.Equal(4m, consumed);
    }

    [Fact]
    public void A_closure_day_inside_a_vacation_range_is_charged_once()
    {
        // The closure costs entitlement anyway, so covering it with vacation charges it once.
        var consumed = WorkingDayCalculator.ConsumedDays(
            new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 6), false, false,
            WeekDays.MondayToFriday, [Closure("2026-03-02")]);

        Assert.Equal(5m, consumed);
    }

    [Fact]
    public void A_day_the_employee_does_not_work_is_never_charged()
    {
        var fourDay = WeekDays.Monday | WeekDays.Tuesday | WeekDays.Wednesday | WeekDays.Thursday;

        // Mon 2 to Fri 6 March on a four-day week: the Friday is not theirs to take.
        var consumed = WorkingDayCalculator.ConsumedDays(
            new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 6), false, false, fourDay);

        Assert.Equal(4m, consumed);
    }
}
