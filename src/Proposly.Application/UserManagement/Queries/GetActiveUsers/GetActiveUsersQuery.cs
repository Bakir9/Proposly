using Proposly.Application.Abstractions;
using Proposly.Application.UserManagement.Responses;

namespace Proposly.Application.UserManagement.Queries.GetActiveUsers;

public record GetActiveUsersQuery : IQuery<IReadOnlyList<UserSummaryResponse>>;
