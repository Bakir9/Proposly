using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Domain.WorkTimeManagement.Services;

namespace Proposly.Domain.Tests.WorkTimeManagement;

public class EmploymentTermsTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private static EmploymentTerms Terms(
        string validFrom = "2026-01-01",
        decimal weeklyHours = 38.5m,
        WeekDays days = WeekDays.MondayToFriday,
        decimal vacationDays = 25m)
        => EmploymentTerms.Create(
            CompanyId, UserId, DateOnly.Parse(validFrom), weeklyHours, days, vacationDays);

    // --- Daily hours ---

    [Theory]
    [InlineData(38.5, 5, 7.7)]
    [InlineData(40.0, 5, 8.0)]
    [InlineData(32.0, 4, 8.0)]
    [InlineData(20.0, 5, 4.0)]
    public void Daily_hours_spread_the_week_over_the_working_pattern(
        decimal weekly, int workingDays, decimal expectedDaily)
    {
        var pattern = workingDays == 4
            ? WeekDays.Monday | WeekDays.Tuesday | WeekDays.Wednesday | WeekDays.Thursday
            : WeekDays.MondayToFriday;

        Assert.Equal(expectedDaily, Terms(weeklyHours: weekly, days: pattern).DailyHours);
    }

    [Fact]
    public void A_four_day_week_gives_longer_days_not_shorter_ones()
    {
        // 32 hours over four days is 8-hour days, not 6.4 — the divisor is the pattern, not five.
        var fourDay = WeekDays.Monday | WeekDays.Tuesday | WeekDays.Wednesday | WeekDays.Thursday;

        Assert.Equal(8.0m, Terms(weeklyHours: 32m, days: fourDay).DailyHours);
    }

    // --- Validation ---

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(61)]
    public void Implausible_weekly_hours_are_rejected(decimal weeklyHours)
    {
        Assert.Throws<ArgumentException>(() => Terms(weeklyHours: weeklyHours));
    }

    [Fact]
    public void An_empty_working_pattern_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => Terms(days: WeekDays.None));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(367)]
    public void Implausible_vacation_entitlement_is_rejected(decimal vacationDays)
    {
        Assert.Throws<ArgumentException>(() => Terms(vacationDays: vacationDays));
    }

    // --- Versioning ---

    [Fact]
    public void Closing_a_version_ends_it_the_day_before_its_successor()
    {
        var january = Terms("2026-01-01");

        january.CloseAt(new DateOnly(2026, 7, 1));

        Assert.Equal(new DateOnly(2026, 6, 30), january.ValidTo);
    }

    [Fact]
    public void A_successor_must_take_effect_after_the_version_it_closes()
    {
        var july = Terms("2026-07-01");

        Assert.Throws<InvalidOperationException>(() => july.CloseAt(new DateOnly(2026, 1, 1)));
        Assert.Throws<InvalidOperationException>(() => july.CloseAt(new DateOnly(2026, 7, 1)));
    }

    [Fact]
    public void History_is_immutable_beyond_being_closed()
    {
        // The only mutator is CloseAt. Nothing exposes weekly hours, the pattern, or the
        // entitlement for change — a correction means a new dated version.
        var mutators = typeof(EmploymentTerms)
            .GetMethods()
            .Where(m => m.DeclaringType == typeof(EmploymentTerms) && !m.IsSpecialName && !m.IsStatic)
            .Select(m => m.Name)
            .ToList();

        Assert.Equal(new[] { nameof(EmploymentTerms.AppliesOn), nameof(EmploymentTerms.CloseAt) },
            mutators.OrderBy(n => n).ToArray());
    }

    [Fact]
    public void An_open_version_applies_from_its_start_onward()
    {
        var terms = Terms("2026-01-01");

        Assert.False(terms.AppliesOn(new DateOnly(2025, 12, 31)));
        Assert.True(terms.AppliesOn(new DateOnly(2026, 1, 1)));
        Assert.True(terms.AppliesOn(new DateOnly(2030, 1, 1)));
    }

    [Fact]
    public void A_closed_version_stops_applying_after_its_end()
    {
        var terms = Terms("2026-01-01");
        terms.CloseAt(new DateOnly(2026, 7, 1));

        Assert.True(terms.AppliesOn(new DateOnly(2026, 6, 30)));
        Assert.False(terms.AppliesOn(new DateOnly(2026, 7, 1)));
    }

    [Fact]
    public void The_version_in_force_on_a_date_is_resolved_from_the_history()
    {
        var january = Terms("2026-01-01", weeklyHours: 38.5m);
        var july = Terms("2026-07-01", weeklyHours: 30m);
        january.CloseAt(july.ValidFrom);
        var history = new[] { january, july };

        Assert.Equal(38.5m, WorkingDayCalculator.TermsOn(history, new DateOnly(2026, 3, 15))!.WeeklyHours);
        Assert.Equal(30m, WorkingDayCalculator.TermsOn(history, new DateOnly(2026, 8, 15))!.WeeklyHours);
        Assert.Null(WorkingDayCalculator.TermsOn(history, new DateOnly(2025, 12, 1)));
    }
}
