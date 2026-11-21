using Proposly.Domain.ProjectManagement.Entities;

namespace Proposly.Domain.ProjectManagement.Repositories;

public interface ITaskTemplateRepository
{
    Task<TaskTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<TaskTemplate>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(TaskTemplate template, CancellationToken ct = default);
    Task RemoveAsync(TaskTemplate template, CancellationToken ct = default);
}
