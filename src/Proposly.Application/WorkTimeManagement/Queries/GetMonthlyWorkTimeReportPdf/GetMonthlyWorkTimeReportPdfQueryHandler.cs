using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Queries.GetMonthlyWorkTimeReport;
using Proposly.Application.WorkTimeManagement.Responses;
using Proposly.Application.WorkTimeManagement.Services;

namespace Proposly.Application.WorkTimeManagement.Queries.GetMonthlyWorkTimeReportPdf;

public sealed class GetMonthlyWorkTimeReportPdfQueryHandler
    : IQueryHandler<GetMonthlyWorkTimeReportPdfQuery, WorkTimeReportPdf?>
{
    private readonly IQueryHandler<GetMonthlyWorkTimeReportQuery, MonthlyWorkTimeReportResponse?> _report;
    private readonly IWorkTimeReportPdfService _pdf;

    public GetMonthlyWorkTimeReportPdfQueryHandler(
        IQueryHandler<GetMonthlyWorkTimeReportQuery, MonthlyWorkTimeReportResponse?> report,
        IWorkTimeReportPdfService pdf)
    {
        _report = report;
        _pdf = pdf;
    }

    public async Task<WorkTimeReportPdf?> HandleAsync(
        GetMonthlyWorkTimeReportPdfQuery query, CancellationToken ct = default)
    {
        // Rendered from the same report the screen shows, so the document and the page can never
        // disagree.
        var report = await _report.HandleAsync(
            new GetMonthlyWorkTimeReportQuery(query.Year, query.Month, query.UserId), ct);

        if (report is null) return null;

        var lastName = report.EmployeeName.Split(' ').LastOrDefault() ?? "employee";
        var safeName = new string(lastName.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

        return new WorkTimeReportPdf(
            _pdf.GenerateMonthlyReportPdf(report),
            $"worktime-{safeName}-{query.Year}-{query.Month:00}.pdf");
    }
}
