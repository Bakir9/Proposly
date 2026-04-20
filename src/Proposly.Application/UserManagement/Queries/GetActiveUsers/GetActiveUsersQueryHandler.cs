using Proposly.Application.Abstractions;
using Proposly.Application.UserManagement.Responses;
using Proposly.Domain.CompanyManagement.Repositories;

namespace Proposly.Application.UserManagement.Queries.GetActiveUsers;

public sealed class GetActiveUsersQueryHandler : IQueryHandler<GetActiveUsersQuery, IReadOnlyList<UserSummaryResponse>>
{
    private readonly IUserRepository _repository;

    public GetActiveUsersQueryHandler(IUserRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<UserSummaryResponse>> HandleAsync(GetActiveUsersQuery query, CancellationToken ct = default)
    {
        var users = await _repository.GetActiveAsync(ct);
        return users.Select(u => new UserSummaryResponse(u.Id, u.FullName, u.Email, u.Role.ToString(), u.IsDisabled, u.CreatedAt)).ToList();
    }
}
