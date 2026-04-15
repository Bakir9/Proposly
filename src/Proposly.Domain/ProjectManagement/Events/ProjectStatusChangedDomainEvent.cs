using Proposly.Domain.ProjectManagement.Enums;
using Proposly.Shared.Primitives;

namespace Proposly.Domain.ProjectManagement.Events;

public sealed record ProjectStatusChangedDomainEvent(Guid ProjectId, ProjectStatus NewStatus) : IDomainEvent;
