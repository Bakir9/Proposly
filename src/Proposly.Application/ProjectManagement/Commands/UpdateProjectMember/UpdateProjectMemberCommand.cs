using Proposly.Application.Abstractions;

namespace Proposly.Application.ProjectManagement.Commands.UpdateProjectMember;

/// <summary>
/// Updates a member's engagement on the project: role, cost rate (what an hour costs the
/// company), bill rate (what the client pays on T&amp;M) and allocated hours. Rates apply to
/// future time entries only — past entries keep their snapshots.
/// </summary>
public sealed record UpdateProjectMemberCommand(
    Guid ProjectId,
    Guid MemberId,
    string Role,
    decimal CostRate,
    decimal BillRate,
    decimal AllocatedHours) : ICommand;
