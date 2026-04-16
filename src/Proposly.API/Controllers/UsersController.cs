using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proposly.API.Authorization;
using Proposly.Application.Abstractions;
using Proposly.Application.UserManagement.Commands.InviteUser;
using Proposly.Application.UserManagement.Commands.RemoveUser;
using Proposly.Application.UserManagement.Commands.UpdateUserRole;
using Proposly.Application.UserManagement.Queries.GetCurrentUser;
using Proposly.Application.UserManagement.Queries.GetUsers;
using Proposly.Application.UserManagement.Responses;

namespace Proposly.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class UsersController : ControllerBase
{
    /// <summary>Get your own profile.</summary>
    [HttpGet("me")]
    public async Task<UserDetailResponse> GetMe(
        [FromServices] IQueryHandler<GetCurrentUserQuery, UserDetailResponse> handler,
        CancellationToken ct)
        => await handler.HandleAsync(new GetCurrentUserQuery(), ct);

    /// <summary>List all users in your company. Owner and Admin only.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.ManageUsers)]
    public async Task<IReadOnlyList<UserSummaryResponse>> GetAll(
        [FromServices] IQueryHandler<GetUsersQuery, IReadOnlyList<UserSummaryResponse>> handler,
        CancellationToken ct)
        => await handler.HandleAsync(new GetUsersQuery(), ct);

    /// <summary>Add a new user to your company. Owner and Admin only.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.ManageUsers)]
    public async Task<ActionResult<UserDetailResponse>> Invite(
        [FromBody] InviteUserCommand command,
        [FromServices] ICommandHandler<InviteUserCommand, UserDetailResponse> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetMe), result);
    }

    /// <summary>Change a user's role. Owner and Admin only.</summary>
    [HttpPut("{id:guid}/role")]
    [Authorize(Policy = Policies.ManageUsers)]
    public async Task<IActionResult> UpdateRole(
        Guid id,
        [FromBody] UpdateRoleRequest request,
        [FromServices] ICommandHandler<UpdateUserRoleCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new UpdateUserRoleCommand(id, request.Role), ct);
        return NoContent();
    }

    /// <summary>Remove a user from your company. Owner only.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.OwnerOnly)]
    public async Task<IActionResult> Remove(
        Guid id,
        [FromServices] ICommandHandler<RemoveUserCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new RemoveUserCommand(id), ct);
        return NoContent();
    }
}

public record UpdateRoleRequest(string Role);
