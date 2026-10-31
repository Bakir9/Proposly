using Proposly.Domain.ProjectManagement.Entities;

namespace Proposly.Domain.ProjectManagement.Repositories;

public interface IProjectFileRepository
{
    Task<ProjectFile?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ProjectFile>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default);
    Task AddAsync(ProjectFile file, CancellationToken ct = default);
    Task RemoveAsync(ProjectFile file, CancellationToken ct = default);
}
