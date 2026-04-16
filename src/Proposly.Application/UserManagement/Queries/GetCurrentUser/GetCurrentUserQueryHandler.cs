using Proposly.Application.Abstractions;
using Proposly.Application.UserManagement.Responses;
using Proposly.Domain.CompanyManagement.Repositories;

namespace Proposly.Application.UserManagement.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler : IQueryHandler<GetCurrentUserQuery, UserDetailResponse>
{
    private readonly IUserRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public GetCurrentUserQueryHandler(IUserRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<UserDetailResponse> HandleAsync(GetCurrentUserQuery query, CancellationToken ct = default)
    {
        var user = await _repository.GetByIdAsync(_currentUser.UserId, ct)
            ?? throw new InvalidOperationException("Current user not found.");

        return new UserDetailResponse(user.Id, user.FirstName, user.LastName, user.FullName, user.Email, user.Role.ToString(), user.CompanyId, user.CreatedAt);
    }
}
