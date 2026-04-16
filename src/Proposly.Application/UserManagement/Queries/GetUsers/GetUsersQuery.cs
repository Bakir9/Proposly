using Proposly.Application.Abstractions;
using Proposly.Application.UserManagement.Responses;

namespace Proposly.Application.UserManagement.Queries.GetUsers;

public record GetUsersQuery : IQuery<IReadOnlyList<UserSummaryResponse>>;
