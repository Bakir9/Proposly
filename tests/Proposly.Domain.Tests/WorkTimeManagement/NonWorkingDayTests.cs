using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;

namespace Proposly.Domain.Tests.WorkTimeManagement;

public class NonWorkingDayTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly DateOnly Date = new(2026, 5, 1);

    private static NonWorkingDay Day(
        NonWorkingDayKind kind = NonWorkingDayKind.PublicHoliday,
        bool? consumesVacation = null,
        string name = "Staatsfeiertag")
        => NonWorkingDay.Create(CompanyId, Date, name, kind, consumesVacation);

    [Fact]
    public void A_public_holiday_costs_the_employee_nothing_by_default()
    {
        var holiday = Day(NonWorkingDayKind.PublicHoliday);

        Assert.False(holiday.ConsumesVacation);
        Assert.Equal(EntrySource.Manual, holiday.Source);
    }

    [Fact]
    public void A_company_closure_consumes_entitlement_by_default()
    {
        // Betriebsurlaub normally comes out of the employee's own allowance.
        Assert.True(Day(NonWorkingDayKind.CompanyClosure, name: "Betriebsurlaub").ConsumesVacation);
    }

    [Fact]
    public void A_closure_can_be_granted_outright()
    {
        // A bridge day the company simply gives away.
        var bridgeDay = Day(NonWorkingDayKind.CompanyClosure, consumesVacation: false, name: "Fenstertag");

        Assert.False(bridgeDay.ConsumesVacation);
    }

    [Fact]
    public void A_public_holiday_that_consumes_vacation_is_refused()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => Day(NonWorkingDayKind.PublicHoliday, consumesVacation: true));

        Assert.Contains("cannot consume vacation", ex.Message);
    }

    [Fact]
    public void The_same_rule_holds_on_update()
    {
        var closure = Day(NonWorkingDayKind.CompanyClosure);

        Assert.Throws<InvalidOperationException>(
            () => closure.Update("Now a holiday", NonWorkingDayKind.PublicHoliday, consumesVacation: true));
    }

    [Fact]
    public void Update_changes_the_name_kind_and_cost()
    {
        var day = Day(NonWorkingDayKind.CompanyClosure);

        day.Update("Corrected name", NonWorkingDayKind.PublicHoliday, consumesVacation: false);

        Assert.Equal("Corrected name", day.Name);
        Assert.Equal(NonWorkingDayKind.PublicHoliday, day.Kind);
        Assert.False(day.ConsumesVacation);
    }

    [Fact]
    public void The_date_cannot_be_changed()
    {
        // Moving a day means delete and recreate, so the one-per-date index stays honest.
        var setters = typeof(NonWorkingDay)
            .GetProperty(nameof(NonWorkingDay.Date))!
            .GetSetMethod(nonPublic: false);

        Assert.Null(setters);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_name_is_rejected(string name)
    {
        Assert.Throws<ArgumentException>(() => Day(name: name));
    }

    [Fact]
    public void An_overlong_name_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => Day(name: new string('x', 201)));
    }

    [Fact]
    public void The_name_is_trimmed()
    {
        Assert.Equal("Staatsfeiertag", Day(name: "  Staatsfeiertag  ").Name);
    }
}
