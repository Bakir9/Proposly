using Proposly.Application.Abstractions;
using Proposly.Application.Hr.Queries.GetWorkers;
using Proposly.Application.Hr.Responses;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.HrManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.Hr.Queries.GetWorker;

public sealed class GetWorkerQueryHandler(
    IUserRepository users,
    IEmployeeProfileRepository profiles,
    IEmploymentTermsRepository termsRepository,
    IAbsenceRepository absences) : IQueryHandler<GetWorkerQuery, WorkerResponse?>
{
    public async Task<WorkerResponse?> HandleAsync(GetWorkerQuery query, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(query.UserId, ct);
        if (user is null)
            return null;

        var profile = await profiles.GetByUserIdAsync(query.UserId, ct);
        var terms = await termsRepository.GetLatestAsync(query.UserId, ct);

        var year = DateTime.UtcNow.Year;
        var entitlement = await absences.GetEntitlementAsync(query.UserId, year, ct);
        var yearAbsences = (await absences.GetForYearAsync(year, ct: ct))
            .Where(a => a.UserId == query.UserId && a.Status != AbsenceStatus.Cancelled)
            .OrderByDescending(a => a.StartDate)
            .Select(a => new WorkerAbsenceResponse(
                a.Id, a.Type.ToString(), a.StartDate, a.EndDate, a.ConsumedDays, a.Status.ToString()))
            .ToList();

        IReadOnlyList<decimal?> dayHours = terms is null
            ? new decimal?[7]
            : [terms.MondayHours, terms.TuesdayHours, terms.WednesdayHours, terms.ThursdayHours, terms.FridayHours, terms.SaturdayHours, terms.SundayHours];

        return new WorkerResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.IsProfileOnly ? null : user.Email,
            user.Role.ToString(),
            GetWorkersQueryHandler.AccountStatusOf(user),
            profile?.JobTitle,
            profile?.Phone,
            profile?.Gender,
            profile?.Birthday,
            profile?.Svnr,
            profile?.Nationality,
            profile?.Department,
            profile?.Location,
            profile?.Address,
            profile?.EmergencyName,
            profile?.EmergencyRelation,
            profile?.EmergencyPhone,
            profile?.EmploymentStart ?? terms?.ValidFrom,
            profile?.EmploymentEnd,
            profile?.PhotoStorageKey is not null,
            profile?.CvJson,
            profile?.CvFileName,
            profile?.CvFileSize,
            profile?.CvFileUploadedAt,
            (terms?.EmploymentType ?? EmploymentType.FullTime).ToString(),
            (terms?.WorkModel ?? WorkModel.FullTime).ToString(),
            terms?.WeeklyHours ?? 0,
            dayHours,
            terms?.HourlyRate?.Amount,
            terms?.HourlyRate?.Currency,
            terms?.MaxHoursPerMonth,
            terms?.ExpectedWeeklyHours,
            (terms?.OvertimeMode ?? OvertimeMode.TimeOff).ToString(),
            terms?.OvertimeSurchargePct,
            terms?.NightSurchargePct,
            terms?.SundaySurchargePct,
            terms?.HolidaySurchargePct,
            terms?.TrackingRequired ?? true,
            terms?.AutoDeductBreak ?? true,
            terms?.Flexitime ?? false,
            terms?.OnCallDuty ?? false,
            terms?.CoreFrom,
            terms?.CoreTo,
            terms?.HomeOfficeDaysPerWeek,
            terms?.MaxHoursPerDay,
            entitlement?.EntitledDays ?? terms?.AnnualVacationDays ?? 25,
            entitlement?.CarriedOverDays ?? 0,
            yearAbsences,
            (profile?.Allowances ?? []).Select(a => new WorkerAllowanceResponse(
                a.Name, a.Amount.Amount, a.Amount.Currency, a.Unit)).ToList(),
            (profile?.Notes ?? []).Select(n => new WorkerNoteResponse(
                n.Id, n.Tag, n.Text, DateOnly.FromDateTime(n.CreatedAt), n.Pinned)).ToList(),
            (profile?.Documents ?? []).Select(d => new WorkerDocumentResponse(
                d.Id, d.Category, d.FileName, d.SizeBytes, DateOnly.FromDateTime(d.CreatedAt))).ToList());
    }
}
