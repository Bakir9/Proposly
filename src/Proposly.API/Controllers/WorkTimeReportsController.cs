using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proposly.API.Authorization;
using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Queries.GetCompanyMonthOverview;
using Proposly.Application.WorkTimeManagement.Queries.GetMonthlyWorkTimeReport;
using Proposly.Application.WorkTimeManagement.Queries.GetMonthlyWorkTimeReportPdf;
using Proposly.Application.WorkTimeManagement.Queries.GetReconciliation;
using Proposly.Application.WorkTimeManagement.Responses;

namespace Proposly.API.Controllers;

/// <summary>
/// Month-end reporting. A separate controller from Reports so the two modules stay independent.
/// <para>
/// Per-employee visibility comes from the query filter: a member can only produce their own
/// report, because the underlying timesheet is invisible to them.
/// </para>
/// </summary>
[Authorize(Policy = Policies.RecordOwnWorkTime)]
[ApiController]
[Route("api/worktime-reports")]
public sealed class WorkTimeReportsController : ControllerBase
{
    [HttpGet("monthly/{year:int}/{month:int}")]
    public async Task<ActionResult<MonthlyWorkTimeReportResponse>> GetMonthly(
        int year,
        int month,
        [FromQuery] Guid? userId,
        [FromServices] IQueryHandler<GetMonthlyWorkTimeReportQuery, MonthlyWorkTimeReportResponse?> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(
            new GetMonthlyWorkTimeReportQuery(year, month, userId), ct);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>The month-end record as a document. A provisional month is watermarked.</summary>
    [HttpGet("monthly/{year:int}/{month:int}/pdf")]
    public async Task<IActionResult> GetMonthlyPdf(
        int year,
        int month,
        [FromQuery] Guid? userId,
        [FromServices] IQueryHandler<GetMonthlyWorkTimeReportPdfQuery, WorkTimeReportPdf?> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(
            new GetMonthlyWorkTimeReportPdfQuery(year, month, userId), ct);

        return result is null ? NotFound() : File(result.Content, "application/pdf", result.FileName);
    }

    [HttpGet("company/{year:int}/{month:int}")]
    [Authorize(Policy = Policies.ViewAllWorkTime)]
    public async Task<IReadOnlyList<CompanyMonthOverviewResponse>> GetCompanyOverview(
        int year,
        int month,
        [FromServices] IQueryHandler<GetCompanyMonthOverviewQuery, IReadOnlyList<CompanyMonthOverviewResponse>> handler,
        CancellationToken ct)
        => await handler.HandleAsync(new GetCompanyMonthOverviewQuery(year, month), ct);

    /// <summary>
    /// Recorded working hours against hours booked to projects. Strictly read-only — no project
    /// cost or profitability figure is touched.
    /// </summary>
    [HttpGet("reconciliation/{year:int}/{month:int}")]
    [Authorize(Policy = Policies.ViewAllWorkTime)]
    public async Task<ActionResult<ReconciliationResponse>> GetReconciliation(
        int year,
        int month,
        [FromQuery] Guid? userId,
        [FromServices] IQueryHandler<GetReconciliationQuery, ReconciliationResponse?> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetReconciliationQuery(year, month, userId), ct);

        return result is null ? NotFound() : Ok(result);
    }
}
