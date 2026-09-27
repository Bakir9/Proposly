using Proposly.Application.WorkTimeManagement.Responses;

namespace Proposly.Application.WorkTimeManagement.Services;

public interface IWorkTimeReportPdfService
{
    /// <summary>
    /// The month-end record as a self-contained document for payroll and for the employee's own
    /// files. A provisional month is watermarked, so a work in progress cannot be mistaken for a
    /// final figure.
    /// </summary>
    byte[] GenerateMonthlyReportPdf(MonthlyWorkTimeReportResponse report);
}
