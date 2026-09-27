using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;

namespace Proposly.Application.WorkTimeManagement.Queries.GetPolicyDefaults;

/// <summary>
/// Unsaved seed values for a jurisdiction, so an admin edits shipped defaults rather than typing
/// every limit. Null when the jurisdiction has no defaults.
/// </summary>
public sealed record GetPolicyDefaultsQuery(string Jurisdiction) : IQuery<WorkTimePolicyResponse?>;
