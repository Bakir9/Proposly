namespace Proposly.Application.Search;

public sealed record SearchResultItem(
    Guid Id,
    string Type,
    string Title,
    string Subtitle,
    string Link);
