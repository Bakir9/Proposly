using Proposly.Domain.WorkTimeManagement.Entities;

namespace Proposly.Domain.Tests.WorkTimeManagement;

public class WorkDayEntryTests
{
    private static readonly Guid TimesheetId = Guid.NewGuid();
    private static readonly DateOnly Date = new(2026, 3, 2);

    private static WorkDayEntry Create(
        string start, string end, int breakMinutes, bool crossesMidnight = false, string? note = null)
        => WorkDayEntry.Create(
            TimesheetId, Date, TimeOnly.Parse(start), TimeOnly.Parse(end), breakMinutes, crossesMidnight, note);

    [Theory]
    [InlineData("08:30", "17:00", 30, 8.0)]
    [InlineData("08:15", "16:45", 45, 7.75)]
    [InlineData("09:00", "17:30", 0, 8.5)]
    [InlineData("07:00", "12:00", 15, 4.75)]
    public void Worked_hours_are_the_span_less_the_break(
        string start, string end, int breakMinutes, decimal expected)
    {
        Assert.Equal(expected, Create(start, end, breakMinutes).WorkedHours);
    }

    [Fact]
    public void A_shift_crossing_midnight_adds_a_day_and_stays_on_the_start_date()
    {
        var entry = Create("22:00", "06:00", 30, crossesMidnight: true);

        Assert.Equal(7.5m, entry.WorkedHours);
        Assert.Equal(Date, entry.Date);
        Assert.True(entry.CrossesMidnight);
    }

    [Fact]
    public void End_before_start_is_rejected_when_not_marked_as_crossing_midnight()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Create("17:00", "08:30", 30));

        Assert.Contains("End time must be after start time", ex.Message);
    }

    [Fact]
    public void Equal_start_and_end_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => Create("09:00", "09:00", 0));
    }

    [Fact]
    public void A_break_at_least_as_long_as_the_span_is_rejected()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Create("08:00", "16:00", 600));

        Assert.Contains("shorter than the time between start and end", ex.Message);
    }

    [Fact]
    public void A_break_exactly_equal_to_the_span_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => Create("08:00", "09:00", 60));
    }

    [Fact]
    public void A_negative_break_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => Create("08:00", "16:00", -1));
    }

    [Fact]
    public void A_note_longer_than_500_characters_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => Create("08:00", "16:00", 30, note: new string('x', 501)));
    }

    [Fact]
    public void Update_replaces_the_times_and_recomputes_hours()
    {
        var entry = Create("08:30", "17:00", 30);

        entry.Update(TimeOnly.Parse("08:15"), TimeOnly.Parse("16:45"), 45, false, "corrected");

        Assert.Equal(7.75m, entry.WorkedHours);
        Assert.Equal("corrected", entry.Note);
    }
}
