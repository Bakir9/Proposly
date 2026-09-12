using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;

namespace Proposly.Domain.Tests.WorkTimeManagement;

public class EmploymentTypeTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Jan1 = new(2026, 1, 1);

    private static EmploymentTerms Terms(
        EmploymentType type = EmploymentType.FullTime,
        decimal weeklyHours = 38.5m,
        bool isAllIn = false,
        decimal? lumpSum = null)
        => EmploymentTerms.Create(
            CompanyId, UserId, Jan1, weeklyHours, WeekDays.MondayToFriday, 25m,
            type, isAllIn, lumpSum);

    [Fact]
    public void The_type_defaults_to_full_time_and_is_recorded()
    {
        Assert.Equal(EmploymentType.FullTime, Terms().EmploymentType);
        Assert.Equal(EmploymentType.PartTime, Terms(EmploymentType.PartTime).EmploymentType);
    }

    [Theory]
    [InlineData(EmploymentType.FullTime, 40)]
    [InlineData(EmploymentType.PartTime, 20)]
    [InlineData(EmploymentType.MarginalEmployment, 8)]
    [InlineData(EmploymentType.Apprentice, 38.5)]
    [InlineData(EmploymentType.Other, 12.5)]
    public void The_type_never_constrains_the_hours(EmploymentType type, decimal weeklyHours)
    {
        // A full-time week is 38.5 hours in one company and 40 in another, and part-time is
        // whatever was agreed. The type suggests; it does not decide.
        var terms = Terms(type, weeklyHours);

        Assert.Equal(weeklyHours, terms.WeeklyHours);
        Assert.Equal(type, terms.EmploymentType);
    }

    [Fact]
    public void Daily_hours_still_follow_the_working_pattern_whatever_the_type()
    {
        var partTime = EmploymentTerms.Create(
            CompanyId, UserId, Jan1, 20m,
            WeekDays.Monday | WeekDays.Tuesday | WeekDays.Wednesday, 25m,
            EmploymentType.PartTime);

        Assert.Equal(6.6667m, partTime.DailyHours);
    }

    // --- Contract options ---

    [Fact]
    public void A_contract_carries_no_compensation_terms_by_default()
    {
        var terms = Terms();

        Assert.False(terms.IsAllIn);
        Assert.Null(terms.OvertimeLumpSumHours);
    }

    [Fact]
    public void An_all_in_contract_is_recorded()
    {
        Assert.True(Terms(isAllIn: true).IsAllIn);
    }

    [Fact]
    public void An_overtime_lump_sum_is_recorded()
    {
        Assert.Equal(10m, Terms(lumpSum: 10m).OvertimeLumpSumHours);
    }

    [Fact]
    public void All_in_and_a_lump_sum_together_are_refused()
    {
        // All-in already covers every additional hour; a lump sum on top would compensate the
        // same overtime twice.
        var ex = Assert.Throws<InvalidOperationException>(
            () => Terms(isAllIn: true, lumpSum: 10m));

        Assert.Contains("cannot also carry an overtime lump sum", ex.Message);
    }

    [Fact]
    public void An_all_in_contract_with_a_zero_lump_sum_is_fine()
    {
        // Zero is the absence of a lump sum, not a contradictory one.
        var terms = Terms(isAllIn: true, lumpSum: 0m);

        Assert.True(terms.IsAllIn);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(201)]
    public void An_implausible_lump_sum_is_refused(decimal lumpSum)
    {
        Assert.Throws<ArgumentException>(() => Terms(lumpSum: lumpSum));
    }
}
