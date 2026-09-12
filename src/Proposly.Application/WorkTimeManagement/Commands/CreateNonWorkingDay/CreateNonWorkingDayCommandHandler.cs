using Proposly.Application.Abstractions;
using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Commands.CreateNonWorkingDay;

public sealed class CreateNonWorkingDayCommandHandler
    : ICommandHandler<CreateNonWorkingDayCommand, Guid>
{
    private readonly INonWorkingDayRepository _calendar;
    private readonly ICurrentUserService _currentUser;

    public CreateNonWorkingDayCommandHandler(
        INonWorkingDayRepository calendar,
        ICurrentUserService currentUser)
    {
        _calendar = calendar;
        _currentUser = currentUser;
    }

    public async Task<Guid> HandleAsync(
        CreateNonWorkingDayCommand command, CancellationToken ct = default)
    {
        // One non-working day per date. This is also what keeps a closure overlapping a holiday
        // counted once rather than twice.
        var existing = await _calendar.GetByDateAsync(command.Date, ct);

        if (existing is not null)
            throw new InvalidOperationException(
                $"{command.Date:yyyy-MM-dd} is already marked as \"{existing.Name}\".");

        var day = NonWorkingDay.Create(
            _currentUser.CompanyId, command.Date, command.Name, command.Kind, command.ConsumesVacation);

        await _calendar.AddAsync(day, ct);

        return day.Id;
    }
}
