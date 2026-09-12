using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Services;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Domain.WorkTimeManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Services;

namespace Proposly.Application.WorkTimeManagement.Commands.RequestAbsence;

public sealed class RequestAbsenceCommandHandler : ICommandHandler<RequestAbsenceCommand, Guid>
{
    private readonly IAbsenceRepository _absences;
    private readonly IEmploymentTermsRepository _terms;
    private readonly INonWorkingDayRepository _calendar;
    private readonly ICompanyRepository _companies;
    private readonly ICurrentUserService _currentUser;

    public RequestAbsenceCommandHandler(
        IAbsenceRepository absences,
        IEmploymentTermsRepository terms,
        INonWorkingDayRepository calendar,
        ICompanyRepository companies,
        ICurrentUserService currentUser)
    {
        _absences = absences;
        _terms = terms;
        _calendar = calendar;
        _companies = companies;
        _currentUser = currentUser;
    }

    public async Task<Guid> HandleAsync(RequestAbsenceCommand command, CancellationToken ct = default)
    {
        await WorkTimeModuleGuard.EnsureEnabledAsync(_companies, _currentUser.CompanyId, ct);

        var overlapping = await _absences.GetOverlappingAsync(
            _currentUser.UserId, command.StartDate, command.EndDate, ct);

        if (overlapping.Count > 0)
        {
            var clash = overlapping[0];
            throw new InvalidOperationException(
                $"This overlaps an existing {clash.Status.ToString().ToLowerInvariant()} absence " +
                $"from {clash.StartDate:yyyy-MM-dd} to {clash.EndDate:yyyy-MM-dd}.");
        }

        // Counted against the employee's real working pattern and the company calendar, so a
        // public holiday inside the range costs nothing and a day they never work is not charged.
        var (pattern, calendar) = await WorkCalendarContext.LoadAsync(
            _currentUser.UserId, command.StartDate, command.EndDate, _terms, _calendar, ct);

        // Calculated here rather than trusted from the client, and then frozen on the row.
        var consumedDays = WorkingDayCalculator.ConsumedDays(
            command.StartDate, command.EndDate,
            command.FirstDayIsHalf, command.LastDayIsHalf,
            pattern, calendar);

        if (consumedDays == 0m)
            throw new InvalidOperationException(
                "That range contains no working days, so there is nothing to request.");

        // Only vacation draws on entitlement, so only vacation can be refused for lack of it.
        if (command.Type == AbsenceType.Vacation)
        {
            var entitlement = await _absences.GetEntitlementAsync(
                _currentUser.UserId, command.StartDate.Year, ct);

            if (entitlement is null)
                throw new InvalidOperationException(
                    $"No vacation entitlement is set for {command.StartDate.Year}. " +
                    "Ask an administrator to set it before requesting vacation.");

            if (consumedDays > entitlement.RemainingDays)
                throw new InvalidOperationException(
                    $"Only {entitlement.RemainingDays} vacation day(s) remain for " +
                    $"{command.StartDate.Year}; this request needs {consumedDays}.");
        }

        var request = AbsenceRequest.Create(
            _currentUser.CompanyId,
            _currentUser.UserId,
            command.Type,
            command.StartDate,
            command.EndDate,
            command.FirstDayIsHalf,
            command.LastDayIsHalf,
            consumedDays,
            command.Reason);

        await _absences.AddAsync(request, ct);

        return request.Id;
    }
}
