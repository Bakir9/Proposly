using Proposly.Shared.Primitives;

namespace Proposly.Domain.ProjectManagement.Events;

public sealed record TaskAssignedDomainEvent(Guid TaskId, Guid ProjectId, Guid AssignedMemberId) : IDomainEvent;
