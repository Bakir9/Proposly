using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Queries.GetEmploymentTerms;

public sealed class GetEmploymentTermsQueryHandler
    : IQueryHandler<GetEmploymentTermsQuery, IReadOnlyList<EmploymentTermsResponse>>
{
    private readonly IEmploymentTermsRepository _terms;
    private readonly ICurrentUserService _currentUser;

    public GetEmploymentTermsQueryHandler(
        IEmploymentTermsRepository terms,
        ICurrentUserService currentUser)
    {
        _terms = terms;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<EmploymentTermsResponse>> HandleAsync(
        GetEmploymentTermsQuery query, CancellationToken ct = default)
    {
        var userId = query.UserId ?? _currentUser.UserId;

        var history = await _terms.GetHistoryAsync(userId, ct);

        return history.Select(t => t.ToResponse()).ToList();
    }
}
