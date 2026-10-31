using Proposly.Domain.ProjectManagement.Entities;

namespace Proposly.Domain.ProjectManagement.Repositories;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Project?> GetByIdForWriteAsync(Guid id, CancellationToken ct = default);
    Task<Project?> GetByIdWithTimeEntriesAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Project>> GetAllWithExpensesAsync(CancellationToken ct = default);

    /// <summary>Everything the home dashboard needs in one load: members, tasks, milestones, time entries and expenses.</summary>
    Task<IReadOnlyList<Project>> GetAllForDashboardAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Project>> SearchAsync(string term, CancellationToken ct = default);
    Task AddAsync(Project project, CancellationToken ct = default);
    Task UpdateAsync(Project project, CancellationToken ct = default);
    Task<int> CountByCompanyIdAsync(Guid companyId, CancellationToken ct = default);
}
