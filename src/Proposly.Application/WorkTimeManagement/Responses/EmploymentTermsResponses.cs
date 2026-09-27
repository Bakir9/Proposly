using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;

namespace Proposly.Application.WorkTimeManagement.Responses;

public sealed record EmploymentTermsResponse(
    Guid Id,
    Guid UserId,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    EmploymentType EmploymentType,
    decimal WeeklyHours,
    IReadOnlyList<string> WorkingDays,
    decimal DailyHours,
    decimal AnnualVacationDays,
    bool IsAllIn,
    decimal? OvertimeLumpSumHours);

/// <summary>
/// Derived expected working days and target hours for a month.
/// <paramref name="TargetHours"/> is null when no employment terms cover the month — reported as
/// unavailable rather than zero, which would read as a full month of deficit.
/// </summary>
public sealed record TargetHoursResponse(
    Guid UserId,
    int Year,
    int Month,
    int WorkingDays,
    int NonWorkingDaysExcluded,
    decimal? TargetHours);

public sealed record NonWorkingDayResponse(
    Guid Id,
    DateOnly Date,
    string Name,
    NonWorkingDayKind Kind,
    bool ConsumesVacation,
    EntrySource Source);

public static class EmploymentTermsMappingExtensions
{
    public static EmploymentTermsResponse ToResponse(this EmploymentTerms terms)
        => new(
            terms.Id,
            terms.UserId,
            terms.ValidFrom,
            terms.ValidTo,
            terms.EmploymentType,
            terms.WeeklyHours,
            terms.WorkingDays.ToNames(),
            terms.DailyHours,
            terms.AnnualVacationDays,
            terms.IsAllIn,
            terms.OvertimeLumpSumHours);

    public static NonWorkingDayResponse ToResponse(this NonWorkingDay day)
        => new(day.Id, day.Date, day.Name, day.Kind, day.ConsumesVacation, day.Source);

    /// <summary>
    /// The flags enum as an ordered list of day names, so the wire format stays readable and the
    /// client does not have to know the bit values.
    /// </summary>
    public static IReadOnlyList<string> ToNames(this WeekDays pattern)
    {
        var days = new List<string>();

        foreach (var day in new[]
        {
            WeekDays.Monday, WeekDays.Tuesday, WeekDays.Wednesday, WeekDays.Thursday,
            WeekDays.Friday, WeekDays.Saturday, WeekDays.Sunday
        })
        {
            if (pattern.HasFlag(day)) days.Add(day.ToString());
        }

        return days;
    }
}
