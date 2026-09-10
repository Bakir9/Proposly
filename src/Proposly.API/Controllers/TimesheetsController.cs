using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proposly.API.Authorization;
using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Commands.ApproveTimesheet;
using Proposly.Application.WorkTimeManagement.Commands.DeleteWorkDay;
using Proposly.Application.WorkTimeManagement.Commands.LockTimesheet;
using Proposly.Application.WorkTimeManagement.Commands.ReopenTimesheet;
using Proposly.Application.WorkTimeManagement.Commands.ReturnTimesheet;
using Proposly.Application.WorkTimeManagement.Commands.SubmitTimesheet;
using Proposly.Application.WorkTimeManagement.Commands.UpsertWorkDay;
using Proposly.Application.WorkTimeManagement.Queries.GetCompanyBreaches;
using Proposly.Application.WorkTimeManagement.Queries.GetCompanyMonth;
using Proposly.Application.WorkTimeManagement.Queries.GetMyTimesheet;
using Proposly.Application.WorkTimeManagement.Queries.GetPendingTimesheets;
using Proposly.Application.WorkTimeManagement.Responses;

namespace Proposly.API.Controllers;

/// <summary>
/// Working time recording. Per-employee visibility is enforced by the AppDbContext query filter,
/// not by these routes — a member asking for a colleague's month gets 404, because the row is
/// invisible to them rather than merely forbidden.
/// </summary>
[Authorize(Policy = Policies.RecordOwnWorkTime)]
[ApiController]
[Route("api/[controller]")]
public sealed class TimesheetsController : ControllerBase
{
    // --- Recording (own timesheet) ---

    [HttpGet("{year:int}/{month:int}")]
    public async Task<ActionResult<TimesheetDetailResponse>> GetMine(
        int year,
        int month,
        [FromServices] IQueryHandler<GetMyTimesheetQuery, TimesheetDetailResponse?> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetMyTimesheetQuery(year, month), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{year:int}/{month:int}/days/{date}")]
    public async Task<IActionResult> UpsertDay(
        int year,
        int month,
        DateOnly date,
        [FromBody] UpsertWorkDayRequest body,
        [FromServices] ICommandHandler<UpsertWorkDayCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(
            new UpsertWorkDayCommand(
                year, month, date,
                body.StartTime, body.EndTime, body.BreakMinutes,
                body.CrossesMidnight, body.Note),
            ct);

        return NoContent();
    }

    [HttpDelete("{year:int}/{month:int}/days/{date}")]
    public async Task<IActionResult> DeleteDay(
        int year,
        int month,
        DateOnly date,
        [FromServices] ICommandHandler<DeleteWorkDayCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new DeleteWorkDayCommand(year, month, date), ct);
        return NoContent();
    }

    [HttpPost("{year:int}/{month:int}/submit")]
    public async Task<IActionResult> Submit(
        int year,
        int month,
        [FromServices] ICommandHandler<SubmitTimesheetCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new SubmitTimesheetCommand(year, month), ct);
        return NoContent();
    }

    // --- Approval ---

    [HttpGet("pending")]
    [Authorize(Policy = Policies.ApproveWorkTime)]
    public async Task<IReadOnlyList<TimesheetSummaryResponse>> GetPending(
        [FromServices] IQueryHandler<GetPendingTimesheetsQuery, IReadOnlyList<TimesheetSummaryResponse>> handler,
        CancellationToken ct)
        => await handler.HandleAsync(new GetPendingTimesheetsQuery(), ct);

    /// <summary>
    /// Approves a submitted month. Where it has outstanding working time breaches,
    /// <c>acknowledgeBreaches</c> must be true — approving blind is refused with 400.
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = Policies.ApproveWorkTime)]
    public async Task<IActionResult> Approve(
        Guid id,
        [FromBody] ApproveTimesheetRequest? body,
        [FromServices] ICommandHandler<ApproveTimesheetCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(
            new ApproveTimesheetCommand(id, body?.AcknowledgeBreaches ?? false), ct);

        return NoContent();
    }

    [HttpPost("{id:guid}/return")]
    [Authorize(Policy = Policies.ApproveWorkTime)]
    public async Task<IActionResult> Return(
        Guid id,
        [FromBody] ReturnTimesheetRequest body,
        [FromServices] ICommandHandler<ReturnTimesheetCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new ReturnTimesheetCommand(id, body.Reason), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/lock")]
    [Authorize(Policy = Policies.ApproveWorkTime)]
    public async Task<IActionResult> Lock(
        Guid id,
        [FromServices] ICommandHandler<LockTimesheetCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new LockTimesheetCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/reopen")]
    [Authorize(Policy = Policies.ApproveWorkTime)]
    public async Task<IActionResult> Reopen(
        Guid id,
        [FromServices] ICommandHandler<ReopenTimesheetCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new ReopenTimesheetCommand(id), ct);
        return NoContent();
    }

    // --- Company views ---

    [HttpGet("company/{year:int}/{month:int}")]
    [Authorize(Policy = Policies.ViewAllWorkTime)]
    public async Task<IReadOnlyList<TimesheetSummaryResponse>> GetCompanyMonth(
        int year,
        int month,
        [FromServices] IQueryHandler<GetCompanyMonthQuery, IReadOnlyList<TimesheetSummaryResponse>> handler,
        CancellationToken ct)
        => await handler.HandleAsync(new GetCompanyMonthQuery(year, month), ct);

    /// <summary>Every outstanding working time breach across the company for a month.</summary>
    [HttpGet("company/{year:int}/{month:int}/breaches")]
    [Authorize(Policy = Policies.ViewAllWorkTime)]
    public async Task<IReadOnlyList<BreachOverviewResponse>> GetCompanyBreaches(
        int year,
        int month,
        [FromServices] IQueryHandler<GetCompanyBreachesQuery, IReadOnlyList<BreachOverviewResponse>> handler,
        CancellationToken ct)
        => await handler.HandleAsync(new GetCompanyBreachesQuery(year, month), ct);
}

/// <summary>
/// Body for a day upsert. Year, month and date come from the route, so they are not repeated here.
/// </summary>
public sealed record UpsertWorkDayRequest(
    TimeOnly StartTime,
    TimeOnly EndTime,
    int BreakMinutes,
    bool CrossesMidnight = false,
    string? Note = null);

public sealed record ReturnTimesheetRequest(string? Reason = null);

public sealed record ApproveTimesheetRequest(bool AcknowledgeBreaches = false);
