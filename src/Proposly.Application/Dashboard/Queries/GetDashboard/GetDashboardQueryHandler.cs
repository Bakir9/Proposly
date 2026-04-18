using Proposly.Application.Abstractions;
using Proposly.Application.Dashboard.Responses;
using Proposly.Domain.OfferManagement.Enums;
using Proposly.Domain.OfferManagement.Repositories;
using Proposly.Domain.ProjectManagement.Enums;
using Proposly.Domain.ProjectManagement.Repositories;

namespace Proposly.Application.Dashboard.Queries.GetDashboard;

public sealed class GetDashboardQueryHandler : IQueryHandler<GetDashboardQuery, DashboardResponse>
{
    private readonly IOfferRepository _offerRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IClientRepository _clientRepository;

    public GetDashboardQueryHandler(
        IOfferRepository offerRepository,
        IProjectRepository projectRepository,
        IClientRepository clientRepository)
    {
        _offerRepository = offerRepository;
        _projectRepository = projectRepository;
        _clientRepository = clientRepository;
    }

    public async Task<DashboardResponse> HandleAsync(GetDashboardQuery query, CancellationToken cancellationToken = default)
    {
        var offers = await _offerRepository.GetAllAsync(cancellationToken);
        var projects = await _projectRepository.GetAllAsync(cancellationToken);
        var clients = await _clientRepository.GetAllAsync(cancellationToken);

        var clientMap = clients.ToDictionary(c => c.Id, c => c.Name);

        var openOffers = offers.Where(o => o.Status is OfferStatus.Draft or OfferStatus.Sent).ToList();
        var openOffersCount = openOffers.Count;
        var openOffersValue = openOffers.Sum(o => o.CalculateSubtotal().Amount);
        var currency = openOffers.FirstOrDefault()?.Currency ?? "EUR";

        var acceptedOffers = offers.Where(o => o.Status == OfferStatus.Accepted).ToList();
        var totalLockedRevenue = acceptedOffers.Sum(o => o.CalculateSubtotal().Amount);

        var activeProjectsCount = projects.Count(p => p.Status == ProjectStatus.Active);

        // HoursThisMonth: GetAllAsync does not include TimeEntries — returning 0 to avoid N+1
        const decimal hoursThisMonth = 0m;

        var recentOffers = offers
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .Select(o => new RecentOfferItem(
                o.Id,
                o.Title,
                clientMap.GetValueOrDefault(o.ClientId, "Unknown"),
                o.Status.ToString(),
                o.CalculateSubtotal().Amount,
                o.Currency))
            .ToList();

        var recentProjects = projects
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .Select(p => new RecentProjectItem(
                p.Id,
                p.Name,
                p.Status.ToString(),
                p.Budget.Amount,
                p.Budget.Currency))
            .ToList();

        return new DashboardResponse(
            openOffersCount,
            openOffersValue,
            currency,
            activeProjectsCount,
            totalLockedRevenue,
            hoursThisMonth,
            recentOffers,
            recentProjects);
    }
}
