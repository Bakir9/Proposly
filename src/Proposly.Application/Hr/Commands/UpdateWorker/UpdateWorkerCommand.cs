using Proposly.Application.Abstractions;
using Proposly.Domain.WorkTimeManagement.Enums;

namespace Proposly.Application.Hr.Commands.UpdateWorker;

public sealed record WorkerAllowanceInput(string Name, decimal Amount, string Currency, string Unit);

/// <summary>
/// Saves a worker's HR record in one batch: name, profile, CV, contract, rules, allowances and
/// the current year's vacation entitlement. Contract-relevant changes create a new
/// EmploymentTerms version (same-day edits replace today's version) — history stays immutable.
/// </summary>
public sealed record UpdateWorkerCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    // profile
    string? JobTitle,
    string? Phone,
    string? Gender,
    DateOnly? Birthday,
    string? Svnr,
    string? Nationality,
    string? Department,
    string? Location,
    string? Address,
    string? EmergencyName,
    string? EmergencyRelation,
    string? EmergencyPhone,
    DateOnly? EmploymentStart,
    DateOnly? EmploymentEnd,
    string? CvJson,
    // contract
    EmploymentType EmploymentType,
    WorkModel WorkModel,
    IReadOnlyList<decimal?>? DayHours, // 7 entries, Monday first; null entry = day off
    decimal? HourlyRateAmount,
    string? HourlyRateCurrency,
    decimal? MaxHoursPerMonth,
    decimal? ExpectedWeeklyHours,
    // rules
    OvertimeMode OvertimeMode,
    decimal? OvertimeSurchargePct,
    decimal? NightSurchargePct,
    decimal? SundaySurchargePct,
    decimal? HolidaySurchargePct,
    bool TrackingRequired,
    bool AutoDeductBreak,
    bool Flexitime,
    bool OnCallDuty,
    TimeOnly? CoreFrom,
    TimeOnly? CoreTo,
    decimal? HomeOfficeDaysPerWeek,
    decimal? MaxHoursPerDay,
    // vacation
    decimal AnnualVacationDays,
    decimal CarriedOverDays,
    IReadOnlyList<WorkerAllowanceInput> Allowances) : ICommand;
