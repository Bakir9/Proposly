using Microsoft.EntityFrameworkCore;
using Proposly.Domain.OfferManagement.Entities;
using Proposly.Domain.OfferManagement.Enums;
using Proposly.Domain.OfferManagement.Repositories;

namespace Proposly.Infrastructure.Persistence.Repositories;

public sealed class OfferRepository : IOfferRepository
{
    private readonly AppDbContext _context;

    public OfferRepository(AppDbContext context) => _context = context;

    public async Task<Offer?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Offers
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IReadOnlyList<Offer>> GetAllAsync(CancellationToken ct = default)
        => await _context.Offers
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Offer>> GetByClientAsync(Guid clientId, CancellationToken ct = default)
        => await _context.Offers
            .Where(o => o.ClientId == clientId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Offer>> GetByStatusAsync(OfferStatus status, CancellationToken ct = default)
        => await _context.Offers
            .Where(o => o.Status == status)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(Offer offer, CancellationToken ct = default)
    {
        await _context.Offers.AddAsync(offer, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Offer offer, CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
