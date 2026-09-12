using Proposly.Application.Abstractions;
using Proposly.Domain.WorkTimeManagement.Enums;

namespace Proposly.Application.WorkTimeManagement.Commands.CreateEmploymentTerms;

/// <summary>
/// Creates a new version of an employee's employment terms. There is deliberately no update or
/// delete: a correction is a new dated version, so reported months keep the terms they were
/// judged against.
/// </summary>
public sealed record CreateEmploymentTermsCommand(
    Guid UserId,
    DateOnly ValidFrom,
    decimal WeeklyHours,
    WeekDays WorkingDays,
    decimal AnnualVacationDays) : ICommand<Guid>;
