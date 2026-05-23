using Proposly.Application.Abstractions;
using Proposly.Application.Reports.Responses;

namespace Proposly.Application.Reports.Queries.GetQuarterlyReport;

public record GetQuarterlyReportQuery(int FiscalYear, int Quarter) : IQuery<QuarterlyReportResponse?>;
