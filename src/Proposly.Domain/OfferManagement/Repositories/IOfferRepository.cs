using Proposly.Domain.OfferManagement.Entities;
using Proposly.Domain.OfferManagement.Enums;

namespace Proposly.Domain.OfferManagement.Repositories;

public interface IOfferRepository
{
    Task<Offer?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Offer>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Offer>> GetByClientAsync(Guid clientId, CancellationToken ct = default);
    Task<IReadOnlyList<Offer>> GetByStatusAsync(OfferStatus status, CancellationToken ct = default);
    Task AddAsync(Offer offer, CancellationToken ct = default);
    Task UpdateAsync(Offer offer, CancellationToken ct = default);
}
