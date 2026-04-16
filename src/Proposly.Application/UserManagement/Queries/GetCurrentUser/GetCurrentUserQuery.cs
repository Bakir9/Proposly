using Proposly.Application.Abstractions;
using Proposly.Application.UserManagement.Responses;

namespace Proposly.Application.UserManagement.Queries.GetCurrentUser;

public record GetCurrentUserQuery : IQuery<UserDetailResponse>;
