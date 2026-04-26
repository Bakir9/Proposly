using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proposly.Application.Abstractions;
using Proposly.Application.Search;

namespace Proposly.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class SearchController : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<SearchResultItem>> Search(
        [FromQuery] string q,
        [FromServices] IQueryHandler<SearchQuery, IReadOnlyList<SearchResultItem>> handler,
        CancellationToken ct)
        => await handler.HandleAsync(new SearchQuery(q), ct);
}
