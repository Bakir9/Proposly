using Proposly.Application.Abstractions;

namespace Proposly.Application.WorkTimeManagement.Queries.GetMonthlyWorkTimeReportPdf;

public sealed record GetMonthlyWorkTimeReportPdfQuery(int Year, int Month, Guid? UserId = null)
    : IQuery<WorkTimeReportPdf?>;

/// <summary>The rendered document and the filename it should be offered under.</summary>
public sealed record WorkTimeReportPdf(byte[] Content, string FileName);
