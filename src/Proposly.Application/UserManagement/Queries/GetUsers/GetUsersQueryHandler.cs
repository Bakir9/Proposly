using Proposly.Application.Abstractions;
using Proposly.Application.UserManagement.Responses;
using Proposly.Domain.CompanyManagement.Repositories;

namespace Proposly.Application.UserManagement.Queries.GetUsers;

public sealed class GetUsersQueryHandler : IQueryHandler<GetUsersQuery, IReadOnlyList<UserSummaryResponse>>
{
    private readonly IUserRepository _repository;

    public GetUsersQueryHandler(IUserRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<UserSummaryResponse>> HandleAsync(GetUsersQuery query, CancellationToken ct = default)
    {
        var users = await _repository.GetAllAsync(ct);
        return users.Select(u => new UserSummaryResponse(u.Id, u.FullName, u.Email, u.Role.ToString(), u.CreatedAt)).ToList();
    }
}
