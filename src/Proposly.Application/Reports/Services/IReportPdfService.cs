using Proposly.Application.Reports.Responses;

namespace Proposly.Application.Reports.Services;

public interface IReportPdfService
{
    byte[] GenerateQuarterlyReportPdf(QuarterlyReportResponse report);
}
