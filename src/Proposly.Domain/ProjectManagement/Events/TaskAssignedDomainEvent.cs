using Proposly.Shared.Primitives;

namespace Proposly.Domain.ProjectManagement.Events;

public sealed record TaskAssignedDomainEvent(
    Guid TaskId,
    string TaskTitle,
    Guid ProjectId,
    string ProjectName,
    Guid AssignedMemberId,
    Guid AssignedUserId,
    Guid CompanyId) : IDomainEvent;
