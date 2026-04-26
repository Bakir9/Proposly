using Proposly.Application.Abstractions;
using Proposly.Domain.OfferManagement.Repositories;
using Proposly.Domain.ProjectManagement.Repositories;

namespace Proposly.Application.Search;

public sealed class SearchQueryHandler(
    IProjectRepository projectRepository,
    IOfferRepository offerRepository,
    IClientRepository clientRepository,
    ICurrentUserService currentUserService)
    : IQueryHandler<SearchQuery, IReadOnlyList<SearchResultItem>>
{
    public async Task<IReadOnlyList<SearchResultItem>> HandleAsync(
        SearchQuery query, CancellationToken cancellationToken = default)
    {
        var term = query.Term.Trim();
        if (term.Length < 2)
            return [];

        var results = new List<SearchResultItem>();
        var isAdminOrOwner = currentUserService.Role is "Owner" or "Admin";

        var projects = await projectRepository.SearchAsync(term, cancellationToken);
        results.AddRange(projects.Select(p => new SearchResultItem(
            p.Id, "Project", p.Name, p.ClientName, $"/projects/{p.Id}")));

        if (isAdminOrOwner)
        {
            var offers = await offerRepository.SearchAsync(term, cancellationToken);
            results.AddRange(offers.Select(o => new SearchResultItem(
                o.Id, "Offer", o.Title, o.Status.ToString(), $"/offers/{o.Id}")));

            var clients = await clientRepository.SearchAsync(term, cancellationToken);
            results.AddRange(clients.Select(c => new SearchResultItem(
                c.Id, "Client", c.Name, c.ContactPerson ?? c.Email ?? "", $"/clients/{c.Id}")));
        }

        return results;
    }
}
