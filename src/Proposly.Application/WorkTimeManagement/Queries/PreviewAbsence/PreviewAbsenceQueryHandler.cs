using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;
using Proposly.Application.WorkTimeManagement.Services;
using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Domain.WorkTimeManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Services;

namespace Proposly.Application.WorkTimeManagement.Queries.PreviewAbsence;

public sealed class PreviewAbsenceQueryHandler
    : IQueryHandler<PreviewAbsenceQuery, AbsencePreviewResponse>
{
    private readonly IAbsenceRepository _absences;
    private readonly IEmploymentTermsRepository _terms;
    private readonly INonWorkingDayRepository _calendar;
    private readonly ICurrentUserService _currentUser;

    public PreviewAbsenceQueryHandler(
        IAbsenceRepository absences,
        IEmploymentTermsRepository terms,
        INonWorkingDayRepository calendar,
        ICurrentUserService currentUser)
    {
        _absences = absences;
        _terms = terms;
        _calendar = calendar;
        _currentUser = currentUser;
    }

    public async Task<AbsencePreviewResponse> HandleAsync(
        PreviewAbsenceQuery query, CancellationToken ct = default)
    {
        if (query.EndDate < query.StartDate)
            return new AbsencePreviewResponse(0m, null, false, false, []);

        // Same inputs as the request handler, so the preview cannot promise a figure the request
        // then refuses.
        var (pattern, calendar) = await WorkCalendarContext.LoadAsync(
            _currentUser.UserId, query.StartDate, query.EndDate, _terms, _calendar, ct);

        var consumedDays = WorkingDayCalculator.ConsumedDays(
            query.StartDate, query.EndDate, query.FirstDayIsHalf, query.LastDayIsHalf,
            pattern, calendar);

        var excluded = WorkingDayCalculator.NonWorkingDatesInRange(
            query.StartDate, query.EndDate, pattern, calendar);

        var overlapping = await _absences.GetOverlappingAsync(
            _currentUser.UserId, query.StartDate, query.EndDate, ct);

        // Only vacation draws on entitlement, so only vacation can exceed it.
        if (query.Type != AbsenceType.Vacation)
        {
            return new AbsencePreviewResponse(
                consumedDays, null, false, overlapping.Count > 0, excluded);
        }

        var entitlement = await _absences.GetEntitlementAsync(
            _currentUser.UserId, query.StartDate.Year, ct);

        if (entitlement is null)
        {
            // No entitlement set is a blocking condition for vacation, reported as "exceeds".
            return new AbsencePreviewResponse(
                consumedDays, null, true, overlapping.Count > 0, excluded);
        }

        var remainingAfter = entitlement.RemainingDays - consumedDays;

        return new AbsencePreviewResponse(
            consumedDays,
            remainingAfter,
            remainingAfter < 0m,
            overlapping.Count > 0,
            excluded);
    }
}
