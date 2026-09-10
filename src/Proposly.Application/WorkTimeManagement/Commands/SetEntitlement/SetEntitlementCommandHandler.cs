using Proposly.Application.Abstractions;
using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Commands.SetEntitlement;

public sealed class SetEntitlementCommandHandler : ICommandHandler<SetEntitlementCommand>
{
    private readonly IAbsenceRepository _absences;
    private readonly ICurrentUserService _currentUser;

    public SetEntitlementCommandHandler(
        IAbsenceRepository absences,
        ICurrentUserService currentUser)
    {
        _absences = absences;
        _currentUser = currentUser;
    }

    public async Task HandleAsync(SetEntitlementCommand command, CancellationToken ct = default)
    {
        var existing = await _absences.GetEntitlementAsync(command.UserId, command.Year, ct);

        if (existing is null)
        {
            var entitlement = AbsenceEntitlement.Create(
                _currentUser.CompanyId, command.UserId, command.Year,
                command.EntitledDays, command.CarriedOverDays);

            await _absences.AddEntitlementAsync(entitlement, ct);
            return;
        }

        // Days already used are preserved — adjusting the allowance never rewrites history.
        existing.SetEntitlement(command.EntitledDays, command.CarriedOverDays);

        await _absences.SaveChangesAsync(ct);
    }
}
