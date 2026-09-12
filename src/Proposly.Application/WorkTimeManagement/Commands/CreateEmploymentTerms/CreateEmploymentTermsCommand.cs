using Proposly.Application.Abstractions;
using Proposly.Domain.WorkTimeManagement.Enums;

namespace Proposly.Application.WorkTimeManagement.Commands.CreateEmploymentTerms;

/// <summary>
/// Creates a new version of an employee's employment terms. There is deliberately no update or
/// delete: a correction is a new dated version, so reported months keep the terms they were
/// judged against.
/// </summary>
/// <param name="EmploymentType">
/// The kind of contract. Recorded and reported; it suggests a starting figure for weekly hours in
/// the UI but never constrains <paramref name="WeeklyHours"/>.
/// </param>
/// <param name="IsAllIn">
/// All-in agreement: salary covers additional hours, so surplus is reported but never banked.
/// </param>
/// <param name="OvertimeLumpSumHours">
/// Überstundenpauschale in hours per month. Surplus up to this is absorbed rather than banked.
/// Mutually exclusive with <paramref name="IsAllIn"/>.
/// </param>
public sealed record CreateEmploymentTermsCommand(
    Guid UserId,
    DateOnly ValidFrom,
    decimal WeeklyHours,
    WeekDays WorkingDays,
    decimal AnnualVacationDays,
    EmploymentType EmploymentType = EmploymentType.FullTime,
    bool IsAllIn = false,
    decimal? OvertimeLumpSumHours = null) : ICommand<Guid>;
