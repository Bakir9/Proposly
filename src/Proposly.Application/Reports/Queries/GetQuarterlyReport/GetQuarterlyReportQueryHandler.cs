using Proposly.Application.Abstractions;
using Proposly.Application.Reports.Responses;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.OfferManagement.Enums;
using Proposly.Domain.OfferManagement.Repositories;
using Proposly.Domain.ProjectManagement.Repositories;

namespace Proposly.Application.Reports.Queries.GetQuarterlyReport;

public sealed class GetQuarterlyReportQueryHandler : IQueryHandler<GetQuarterlyReportQuery, QuarterlyReportResponse?>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IOfferRepository _offerRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetQuarterlyReportQueryHandler(
        ICompanyRepository companyRepository,
        IOfferRepository offerRepository,
        IProjectRepository projectRepository,
        ICurrentUserService currentUserService)
    {
        _companyRepository = companyRepository;
        _offerRepository = offerRepository;
        _projectRepository = projectRepository;
        _currentUserService = currentUserService;
    }

    public async Task<QuarterlyReportResponse?> HandleAsync(GetQuarterlyReportQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Quarter is < 1 or > 4)
            throw new InvalidOperationException("Quarter must be between 1 and 4.");

        var company = await _companyRepository.GetByIdAsync(_currentUserService.CompanyId, cancellationToken);
        if (company is null) return null;

        var (startDate, endDate) = ComputeDateRange(query.FiscalYear, query.Quarter, company.FiscalYearStartMonth);
        var startUtc = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endUtc = endDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var offers = await _offerRepository.GetAllAsync(cancellationToken);
        var projects = await _projectRepository.GetAllWithExpensesAsync(cancellationToken);

        var currency = offers.FirstOrDefault()?.Currency
            ?? projects.FirstOrDefault()?.Budget.Currency
            ?? "EUR";

        var offersInRange = offers.Where(o => o.CreatedAt >= startUtc && o.CreatedAt <= endUtc).ToList();

        var created = offersInRange;
        var accepted = offersInRange.Where(o => o.Status == OfferStatus.Accepted).ToList();
        var rejected = offersInRange.Where(o => o.Status == OfferStatus.Rejected).ToList();
        var expired  = offersInRange.Where(o => o.Status == OfferStatus.Expired).ToList();

        var closed = accepted.Count + rejected.Count + expired.Count;
        var conversionRate = closed > 0 ? Math.Round((decimal)accepted.Count / closed * 100, 1) : 0m;

        var revenue = accepted.Sum(o => o.CalculateTotal().Amount);

        var timeEntries = projects
            .SelectMany(p => p.TimeEntries)
            .Where(te => te.Date >= startDate && te.Date <= endDate)
            .ToList();

        var laborCost = timeEntries.Sum(te => te.HoursWorked * te.HourlyRateSnapshot.Amount);
        var totalHours = timeEntries.Sum(te => te.HoursWorked);

        var expenses = projects
            .SelectMany(p => p.Expenses)
            .Where(e => e.Date >= startDate && e.Date <= endDate)
            .ToList();

        var expensesByCategory = expenses
            .GroupBy(e => e.Category.ToString())
            .Select(g => new ExpenseCategoryBreakdown(g.Key, g.Sum(e => e.Amount.Amount)))
            .OrderByDescending(x => x.Amount)
            .ToList();

        var totalExpenses = expenses.Sum(e => e.Amount.Amount);
        var grossProfit = revenue - laborCost - totalExpenses;
        var profitMargin = revenue > 0 ? Math.Round(grossProfit / revenue * 100, 1) : 0m;

        return new QuarterlyReportResponse(
            Quarter: query.Quarter,
            FiscalYear: query.FiscalYear,
            FiscalYearStartMonth: company.FiscalYearStartMonth,
            StartDate: startDate,
            EndDate: endDate,
            Currency: currency,
            OffersCreatedCount: created.Count,
            OffersCreatedValue: created.Sum(o => o.CalculateTotal().Amount),
            OffersAcceptedCount: accepted.Count,
            OffersAcceptedValue: revenue,
            OffersRejectedCount: rejected.Count,
            OffersRejectedValue: rejected.Sum(o => o.CalculateTotal().Amount),
            OffersExpiredCount: expired.Count,
            OffersExpiredValue: expired.Sum(o => o.CalculateTotal().Amount),
            ConversionRate: conversionRate,
            LaborCost: laborCost,
            TotalHoursWorked: totalHours,
            ExpensesByCategory: expensesByCategory,
            TotalExpenses: totalExpenses,
            GrossProfit: grossProfit,
            ProfitMargin: profitMargin);
    }

    private static (DateOnly start, DateOnly end) ComputeDateRange(int fiscalYear, int quarter, int fiscalYearStartMonth)
    {
        // Q1 starts at fiscalYearStartMonth of fiscalYear; each subsequent quarter adds 3 months.
        // If the result wraps past month 12, the calendar year increments.
        int offsetMonths = (quarter - 1) * 3;
        int totalMonthOffset = (fiscalYearStartMonth - 1) + offsetMonths;
        int startMonth = (totalMonthOffset % 12) + 1;
        int startYear = fiscalYear + totalMonthOffset / 12;

        var startDate = new DateOnly(startYear, startMonth, 1);
        var endDate = startDate.AddMonths(3).AddDays(-1);
        return (startDate, endDate);
    }
}
