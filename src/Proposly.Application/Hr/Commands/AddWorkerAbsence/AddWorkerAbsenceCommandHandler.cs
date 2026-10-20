using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Services;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Domain.WorkTimeManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Services;

namespace Proposly.Application.Hr.Commands.AddWorkerAbsence;

public sealed class AddWorkerAbsenceCommandHandler(
    IAbsenceRepository absences,
    IEmploymentTermsRepository terms,
    INonWorkingDayRepository calendar,
    ICompanyRepository companies,
    ICurrentUserService currentUser) : ICommandHandler<AddWorkerAbsenceCommand, Guid>
{
    public async Task<Guid> HandleAsync(AddWorkerAbsenceCommand command, CancellationToken ct = default)
    {
        await WorkTimeModuleGuard.EnsureEnabledAsync(companies, currentUser.CompanyId, ct);

        var overlapping = await absences.GetOverlappingAsync(
            command.UserId, command.StartDate, command.EndDate, ct);
        if (overlapping.Count > 0)
        {
            var clash = overlapping[0];
            throw new InvalidOperationException(
                $"This overlaps an existing {clash.Status.ToString().ToLowerInvariant()} absence " +
                $"from {clash.StartDate:yyyy-MM-dd} to {clash.EndDate:yyyy-MM-dd}.");
        }

        // Same counting rules as a self-service request: the worker's pattern and the company
        // calendar decide what a day costs, never the client.
        var (pattern, workCalendar) = await WorkCalendarContext.LoadAsync(
            command.UserId, command.StartDate, command.EndDate, terms, calendar, ct);
        var consumedDays = WorkingDayCalculator.ConsumedDays(
            command.StartDate, command.EndDate,
            firstDayIsHalf: false, lastDayIsHalf: false,
            pattern, workCalendar);

        if (consumedDays == 0m)
            throw new InvalidOperationException(
                "That range contains no working days, so there is nothing to record.");

        var request = AbsenceRequest.Create(
            currentUser.CompanyId, command.UserId, command.Type,
            command.StartDate, command.EndDate,
            firstDayIsHalf: false, lastDayIsHalf: false,
            consumedDays);

        // Sick leave is a fact, not a request — recorded as approved right away. Entitlement is
        // untouched (only vacation consumes it), so approving here is safe without a draw-down.
        if (command.Type == AbsenceType.SickLeave)
            request.Approve(currentUser.UserId);

        await absences.AddAsync(request, ct);
        await absences.SaveChangesAsync(ct);
        return request.Id;
    }
}
