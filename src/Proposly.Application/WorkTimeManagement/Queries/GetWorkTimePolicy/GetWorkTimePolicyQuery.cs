using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;

namespace Proposly.Application.WorkTimeManagement.Queries.GetWorkTimePolicy;

/// <summary>
/// The policy in force today, with its version history. Null means none is configured — the client
/// must prompt an owner rather than assume there are no limits.
/// </summary>
public sealed record GetWorkTimePolicyQuery : IQuery<WorkTimePolicyResponse?>;
