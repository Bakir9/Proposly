using Proposly.Application.Abstractions;

namespace Proposly.Application.WorkTimeManagement.Commands.UpsertWorkDay;

/// <summary>
/// Records or replaces one working day in the caller's own timesheet. Idempotent on
/// <paramref name="Date"/>. Set <paramref name="CrossesMidnight"/> for a night shift, where the
/// end time legitimately falls at or before the start time.
/// </summary>
public sealed record UpsertWorkDayCommand(
    int Year,
    int Month,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int BreakMinutes,
    bool CrossesMidnight = false,
    string? Note = null) : ICommand;
