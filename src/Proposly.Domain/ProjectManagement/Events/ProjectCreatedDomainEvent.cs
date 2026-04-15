using Proposly.Shared.Primitives;

namespace Proposly.Domain.ProjectManagement.Events;

public sealed record ProjectCreatedDomainEvent(Guid ProjectId, Guid CompanyId) : IDomainEvent;
