using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proposly.API.Authorization;
using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Commands.CreateEmploymentTerms;
using Proposly.Application.WorkTimeManagement.Commands.CreateNonWorkingDay;
using Proposly.Application.WorkTimeManagement.Commands.CreateWorkTimePolicy;
using Proposly.Application.WorkTimeManagement.Commands.DeleteNonWorkingDay;
using Proposly.Application.WorkTimeManagement.Commands.UpdateNonWorkingDay;
using Proposly.Application.WorkTimeManagement.Queries.GetEmploymentTerms;
using Proposly.Application.WorkTimeManagement.Queries.GetNonWorkingDays;
using Proposly.Application.WorkTimeManagement.Queries.GetPolicyDefaults;
using Proposly.Application.WorkTimeManagement.Queries.GetTargetHours;
using Proposly.Application.WorkTimeManagement.Queries.GetWorkTimePolicy;
using Proposly.Application.WorkTimeManagement.Responses;
using Proposly.Domain.WorkTimeManagement.Enums;

namespace Proposly.API.Controllers;

/// <summary>
/// Company-level working time configuration. Employment terms and the holiday calendar join this
/// controller in a later phase.
/// </summary>
[Authorize(Policy = Policies.ManageWorkTimeSettings)]
[ApiController]
[Route("api/worktime-settings")]
public sealed class WorkTimeSettingsController : ControllerBase
{
    /// <summary>
    /// The policy in force, with its version history. 404 means none is configured, which the
    /// client must present as a prompt rather than as "no limits apply".
    /// </summary>
    [HttpGet("policy")]
    [Authorize(Policy = Policies.RecordOwnWorkTime)]
    public async Task<ActionResult<WorkTimePolicyResponse>> GetPolicy(
        [FromServices] IQueryHandler<GetWorkTimePolicyQuery, WorkTimePolicyResponse?> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetWorkTimePolicyQuery(), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Unsaved seed values for a jurisdiction, for the policy form.</summary>
    [HttpGet("policy/defaults")]
    public async Task<ActionResult<WorkTimePolicyResponse>> GetPolicyDefaults(
        [FromQuery] string jurisdiction,
        [FromServices] IQueryHandler<GetPolicyDefaultsQuery, WorkTimePolicyResponse?> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetPolicyDefaultsQuery(jurisdiction), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("policy")]
    public async Task<ActionResult<Guid>> CreatePolicy(
        [FromBody] CreateWorkTimePolicyCommand command,
        [FromServices] ICommandHandler<CreateWorkTimePolicyCommand, Guid> handler,
        CancellationToken ct)
    {
        var id = await handler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetPolicy), new { }, id);
    }

    // --- Employment terms ---

    /// <summary>Own terms history. Any employee may read their own.</summary>
    [HttpGet("terms/mine")]
    [Authorize(Policy = Policies.RecordOwnWorkTime)]
    public async Task<IReadOnlyList<EmploymentTermsResponse>> GetMyTerms(
        [FromServices] IQueryHandler<GetEmploymentTermsQuery, IReadOnlyList<EmploymentTermsResponse>> handler,
        CancellationToken ct)
        => await handler.HandleAsync(new GetEmploymentTermsQuery(), ct);

    [HttpGet("terms/{userId:guid}")]
    public async Task<IReadOnlyList<EmploymentTermsResponse>> GetTerms(
        Guid userId,
        [FromServices] IQueryHandler<GetEmploymentTermsQuery, IReadOnlyList<EmploymentTermsResponse>> handler,
        CancellationToken ct)
        => await handler.HandleAsync(new GetEmploymentTermsQuery(userId), ct);

    /// <summary>
    /// Creates a new terms version. There is no update or delete: history is immutable, so a
    /// correction is a new dated version.
    /// </summary>
    [HttpPost("terms/{userId:guid}")]
    public async Task<ActionResult<Guid>> CreateTerms(
        Guid userId,
        [FromBody] CreateEmploymentTermsRequest body,
        [FromServices] ICommandHandler<CreateEmploymentTermsCommand, Guid> handler,
        CancellationToken ct)
    {
        var id = await handler.HandleAsync(
            new CreateEmploymentTermsCommand(
                userId, body.ValidFrom, body.WeeklyHours, body.WorkingDays, body.AnnualVacationDays),
            ct);

        return CreatedAtAction(nameof(GetTerms), new { userId }, id);
    }

    /// <summary>Derived expected working days and target hours for a month.</summary>
    [HttpGet("terms/{userId:guid}/target")]
    public async Task<TargetHoursResponse> GetTargetHours(
        Guid userId,
        [FromQuery] int year,
        [FromQuery] int month,
        [FromServices] IQueryHandler<GetTargetHoursQuery, TargetHoursResponse> handler,
        CancellationToken ct)
        => await handler.HandleAsync(new GetTargetHoursQuery(year, month, userId), ct);

    [HttpGet("terms/mine/target")]
    [Authorize(Policy = Policies.RecordOwnWorkTime)]
    public async Task<TargetHoursResponse> GetMyTargetHours(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromServices] IQueryHandler<GetTargetHoursQuery, TargetHoursResponse> handler,
        CancellationToken ct)
        => await handler.HandleAsync(new GetTargetHoursQuery(year, month), ct);

    // --- Holiday and closure calendar ---

    /// <summary>
    /// Readable by every employee: the calendar shapes their own target hours and absence counts,
    /// and the frontend renders it as markers in the shared calendar view.
    /// </summary>
    [HttpGet("non-working-days")]
    [Authorize(Policy = Policies.RecordOwnWorkTime)]
    public async Task<IReadOnlyList<NonWorkingDayResponse>> GetNonWorkingDays(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromServices] IQueryHandler<GetNonWorkingDaysQuery, IReadOnlyList<NonWorkingDayResponse>> handler,
        CancellationToken ct)
        => await handler.HandleAsync(new GetNonWorkingDaysQuery(from, to), ct);

    [HttpPost("non-working-days")]
    public async Task<ActionResult<Guid>> CreateNonWorkingDay(
        [FromBody] CreateNonWorkingDayCommand command,
        [FromServices] ICommandHandler<CreateNonWorkingDayCommand, Guid> handler,
        CancellationToken ct)
    {
        var id = await handler.HandleAsync(command, ct);

        return CreatedAtAction(
            nameof(GetNonWorkingDays),
            new { from = command.Date, to = command.Date },
            id);
    }

    /// <summary>The date is immutable — delete and recreate to move a day.</summary>
    [HttpPut("non-working-days/{id:guid}")]
    public async Task<IActionResult> UpdateNonWorkingDay(
        Guid id,
        [FromBody] UpdateNonWorkingDayRequest body,
        [FromServices] ICommandHandler<UpdateNonWorkingDayCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(
            new UpdateNonWorkingDayCommand(id, body.Name, body.Kind, body.ConsumesVacation), ct);

        return NoContent();
    }

    [HttpDelete("non-working-days/{id:guid}")]
    public async Task<IActionResult> DeleteNonWorkingDay(
        Guid id,
        [FromServices] ICommandHandler<DeleteNonWorkingDayCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new DeleteNonWorkingDayCommand(id), ct);
        return NoContent();
    }
}

public sealed record CreateEmploymentTermsRequest(
    DateOnly ValidFrom,
    decimal WeeklyHours,
    WeekDays WorkingDays,
    decimal AnnualVacationDays);

public sealed record UpdateNonWorkingDayRequest(
    string Name,
    NonWorkingDayKind Kind,
    bool ConsumesVacation);
