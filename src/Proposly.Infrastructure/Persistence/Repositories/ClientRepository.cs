using Microsoft.EntityFrameworkCore;
using Proposly.Domain.OfferManagement.Entities;
using Proposly.Domain.OfferManagement.Repositories;

namespace Proposly.Infrastructure.Persistence.Repositories;

public sealed class ClientRepository : IClientRepository
{
    private readonly AppDbContext _context;

    public ClientRepository(AppDbContext context) => _context = context;

    public async Task<Client?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Clients.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Client>> GetAllAsync(CancellationToken ct = default)
        => await _context.Clients
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Client>> SearchAsync(string term, CancellationToken ct = default)
        => await _context.Clients
            .Where(c => EF.Functions.ILike(c.Name, $"%{term}%") ||
                        (c.ContactPerson != null && EF.Functions.ILike(c.ContactPerson, $"%{term}%")))
            .OrderBy(c => c.Name)
            .Take(5)
            .ToListAsync(ct);

    public async Task AddAsync(Client client, CancellationToken ct = default)
    {
        await _context.Clients.AddAsync(client, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Client client, CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
