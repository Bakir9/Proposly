using Microsoft.EntityFrameworkCore;
using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.CompanyManagement.Enums;
using Proposly.Domain.CompanyManagement.Repositories;

namespace Proposly.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context) => _context = context;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    // Email lookup must bypass the global tenant filter (user is not yet authenticated)
    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task<User?> GetByResetTokenAsync(string token, CancellationToken ct = default)
        => await _context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.PasswordResetToken == token, ct);

    public async Task<User?> GetByInviteTokenAsync(string token, CancellationToken ct = default)
        => await _context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.InviteToken == token, ct);

    public async Task<User?> GetByEmailVerificationTokenAsync(string token, CancellationToken ct = default)
        => await _context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.EmailVerificationToken == token, ct);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        => await _context.Users.IgnoreQueryFilters()
            .AnyAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
        => await _context.Users
            .OrderBy(u => u.FirstName)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<User>> GetActiveAsync(CancellationToken ct = default)
        => await _context.Users
            .Where(u => !u.IsDisabled)
            .OrderBy(u => u.FirstName)
            .ToListAsync(ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await _context.Users.AddAsync(user, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(User user, CancellationToken ct = default)
    {
        _context.Users.Remove(user);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<int> CountInvitedByCompanyIdAsync(Guid companyId, CancellationToken ct = default)
        => await _context.Users
            .Where(u => u.CompanyId == companyId && !u.IsDisabled && u.Role != UserRole.Owner)
            .CountAsync(ct);
}
