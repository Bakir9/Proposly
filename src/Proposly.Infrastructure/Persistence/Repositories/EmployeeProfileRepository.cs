using Microsoft.EntityFrameworkCore;
using Proposly.Domain.HrManagement.Entities;
using Proposly.Domain.HrManagement.Repositories;

namespace Proposly.Infrastructure.Persistence.Repositories;

public sealed class EmployeeProfileRepository(AppDbContext context) : IEmployeeProfileRepository
{
    public async Task<EmployeeProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await context.EmployeeProfiles
            .Include(p => p.Documents)
            .Include(p => p.Allowances)
            .Include(p => p.Notes)
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

    public async Task<IReadOnlyList<EmployeeProfile>> GetAllAsync(CancellationToken ct = default)
        => await context.EmployeeProfiles
            .Include(p => p.Documents)
            .Include(p => p.Allowances)
            .Include(p => p.Notes)
            .ToListAsync(ct);

    public async Task AddAsync(EmployeeProfile profile, CancellationToken ct = default)
        => await context.EmployeeProfiles.AddAsync(profile, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => context.SaveChangesAsync(ct);
}
