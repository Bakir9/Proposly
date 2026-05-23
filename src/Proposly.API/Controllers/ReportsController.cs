using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proposly.Application.Abstractions;
using Proposly.Application.Reports.Queries.GetQuarterlyReport;
using Proposly.Application.Reports.Responses;
using Proposly.Application.Reports.Services;

namespace Proposly.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class ReportsController : ControllerBase
{
    [HttpGet("quarterly")]
    public async Task<ActionResult<QuarterlyReportResponse>> GetQuarterly(
        [FromQuery] int fiscalYear,
        [FromQuery] int quarter,
        [FromServices] IQueryHandler<GetQuarterlyReportQuery, QuarterlyReportResponse?> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetQuarterlyReportQuery(fiscalYear, quarter), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("quarterly/pdf")]
    public async Task<IActionResult> GetQuarterlyPdf(
        [FromQuery] int fiscalYear,
        [FromQuery] int quarter,
        [FromServices] IQueryHandler<GetQuarterlyReportQuery, QuarterlyReportResponse?> handler,
        [FromServices] IReportPdfService pdfService,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetQuarterlyReportQuery(fiscalYear, quarter), ct);
        if (result is null) return NotFound();

        var bytes = pdfService.GenerateQuarterlyReportPdf(result);
        var fileName = $"report-Q{quarter}-FY{fiscalYear}.pdf";
        return File(bytes, "application/pdf", fileName);
    }
}
