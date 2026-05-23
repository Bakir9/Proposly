using Proposly.Domain.CalendarManagement.Entities;

namespace Proposly.Domain.CalendarManagement.Repositories;

public interface ITerminRepository
{
    Task<Termin?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Termin>> GetForUserAsync(Guid userId, DateTime start, DateTime end, CancellationToken ct = default);
    Task<IReadOnlyList<Termin>> GetAvailabilityAsync(Guid userId, DateTime start, DateTime end, CancellationToken ct = default);
    Task<IReadOnlyList<Termin>> GetPendingInvitationsAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(Termin termin, CancellationToken ct = default);
    Task UpdateAsync(Termin termin, CancellationToken ct = default);
    Task DeleteAsync(Termin termin, CancellationToken ct = default);
}
