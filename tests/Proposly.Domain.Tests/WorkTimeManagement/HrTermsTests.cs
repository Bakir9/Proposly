using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Shared.ValueObjects;

namespace Proposly.Domain.Tests.WorkTimeManagement;

public class HrTermsTests
{
    private static EmploymentTerms Create(HrTerms? hr = null, bool isAllIn = false)
        => EmploymentTerms.Create(
            Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 1),
            38.5m, WeekDays.MondayToFriday, 25, isAllIn: isAllIn, hr: hr);

    [Fact]
    public void Create_WithoutHrFacet_AppliesDefaults()
    {
        var terms = Create();

        Assert.Equal(WorkModel.FullTime, terms.WorkModel);
        Assert.Equal(OvertimeMode.TimeOff, terms.OvertimeMode);
        Assert.True(terms.TrackingRequired);
        Assert.True(terms.AutoDeductBreak);
        Assert.False(terms.Flexitime);
        Assert.Null(terms.MondayHours);
        Assert.Null(terms.HourlyRate);
    }

    [Fact]
    public void Create_AllInMode_WithoutAllInFlag_Throws()
        => Assert.Throws<InvalidOperationException>(
            () => Create(new HrTerms(OvertimeMode: OvertimeMode.AllIn), isAllIn: false));

    [Fact]
    public void Create_AllInMode_WithAllInFlag_Succeeds()
    {
        var terms = Create(new HrTerms(OvertimeMode: OvertimeMode.AllIn), isAllIn: true);
        Assert.Equal(OvertimeMode.AllIn, terms.OvertimeMode);
        Assert.True(terms.IsAllIn);
    }

    [Fact]
    public void ToHrTerms_RoundTripsTheFacet()
    {
        var hr = new HrTerms(
            WorkModel.PartTime,
            MondayHours: 6, TuesdayHours: 6, WednesdayHours: 6, ThursdayHours: 6,
            OvertimeMode: OvertimeMode.PaidOut,
            OvertimeSurchargePct: 50,
            HourlyRate: new Money(85, "EUR"),
            Flexitime: true,
            CoreFrom: new TimeOnly(9, 30),
            CoreTo: new TimeOnly(15, 0));

        var terms = Create(hr);

        Assert.Equal(hr, terms.ToHrTerms());
    }
}
