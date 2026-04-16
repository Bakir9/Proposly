using Proposly.Domain.OfferManagement.Entities;

namespace Proposly.Domain.OfferManagement.Repositories;

public interface IClientRepository
{
    Task<Client?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Client>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Client client, CancellationToken ct = default);
    Task UpdateAsync(Client client, CancellationToken ct = default);
}
