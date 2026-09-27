using Proposly.Domain.WorkTimeManagement.Entities;

namespace Proposly.Application.WorkTimeManagement.Responses;

public static class TimesheetMappingExtensions
{
    public static TimesheetDetailResponse ToDetail(
        this Timesheet timesheet,
        string employeeName,
        string? approvedByName,
        IReadOnlyList<TimesheetBreach>? breaches = null)
        => new(
            timesheet.Id,
            timesheet.UserId,
            employeeName,
            timesheet.Year,
            timesheet.Month,
            timesheet.Status,
            timesheet.TotalWorkedHours,
            timesheet.IsSelfApproved,
            timesheet.SubmittedAt,
            timesheet.ApprovedAt,
            approvedByName,
            timesheet.ReopenedAt,
            timesheet.Days
                .OrderBy(d => d.Date)
                .Select(d => new WorkDayResponse(
                    d.Id, d.Date, d.StartTime, d.EndTime,
                    d.BreakMinutes, d.CrossesMidnight, d.WorkedHours, d.Note))
                .ToList(),
            (breaches ?? [])
                .OrderBy(b => b.Date ?? b.WeekStartDate)
                .ThenBy(b => b.Kind)
                .Select(b => b.ToResponse())
                .ToList());

    public static TimesheetSummaryResponse ToSummary(
        this Timesheet timesheet, string employeeName, int breachCount = 0)
        => new(
            timesheet.Id,
            timesheet.UserId,
            employeeName,
            timesheet.Year,
            timesheet.Month,
            timesheet.Status,
            timesheet.TotalWorkedHours,
            timesheet.IsSelfApproved,
            timesheet.SubmittedAt,
            timesheet.ApprovedAt,
            breachCount);
}
