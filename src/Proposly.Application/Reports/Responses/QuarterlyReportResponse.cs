namespace Proposly.Application.Reports.Responses;

public record QuarterlyReportResponse(
    int Quarter,
    int FiscalYear,
    int FiscalYearStartMonth,
    DateOnly StartDate,
    DateOnly EndDate,
    string Currency,
    // Offer funnel
    int OffersCreatedCount,
    decimal OffersCreatedValue,
    int OffersAcceptedCount,
    decimal OffersAcceptedValue,
    int OffersRejectedCount,
    decimal OffersRejectedValue,
    int OffersExpiredCount,
    decimal OffersExpiredValue,
    decimal ConversionRate,
    // Project costs in the period
    decimal LaborCost,
    decimal TotalHoursWorked,
    IReadOnlyList<ExpenseCategoryBreakdown> ExpensesByCategory,
    decimal TotalExpenses,
    // Profitability
    decimal GrossProfit,
    decimal ProfitMargin
);

public record ExpenseCategoryBreakdown(string Category, decimal Amount);
