namespace Proposly.Application.Dashboard.Responses;

public record DashboardResponse(
    int OpenOffersCount,
    decimal OpenOffersValue,
    string Currency,
    int ActiveProjectsCount,
    decimal TotalLockedRevenue,
    decimal HoursThisMonth,
    IReadOnlyList<RecentOfferItem> RecentOffers,
    IReadOnlyList<RecentProjectItem> RecentProjects);

public record RecentOfferItem(Guid Id, string Title, string ClientName, string Status, decimal Total, string Currency);
public record RecentProjectItem(Guid Id, string Name, string Status, decimal BudgetAmount, string Currency);
