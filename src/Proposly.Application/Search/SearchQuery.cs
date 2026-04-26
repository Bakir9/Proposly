using Proposly.Application.Abstractions;

namespace Proposly.Application.Search;

public sealed record SearchQuery(string Term) : IQuery<IReadOnlyList<SearchResultItem>>;
