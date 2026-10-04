using Proposly.Shared.Primitives;

namespace Proposly.Domain.ProjectManagement.Events;

public sealed record ProjectMemberAddedDomainEvent(
    Guid ProjectId,
    string ProjectName,
    Guid UserId,
    Guid CompanyId) : IDomainEvent;
