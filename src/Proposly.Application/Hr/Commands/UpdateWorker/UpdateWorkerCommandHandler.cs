using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.HrManagement.Entities;
using Proposly.Domain.HrManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Domain.WorkTimeManagement.Repositories;
using Proposly.Shared.ValueObjects;

namespace Proposly.Application.Hr.Commands.UpdateWorker;

public sealed class UpdateWorkerCommandHandler(
    IUserRepository users,
    IEmployeeProfileRepository profiles,
    IEmploymentTermsRepository termsRepository,
    IAbsenceRepository absences,
    ICurrentUserService currentUser) : ICommandHandler<UpdateWorkerCommand>
{
    public async Task HandleAsync(UpdateWorkerCommand command, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(command.UserId, ct)
            ?? throw new InvalidOperationException($"Worker {command.UserId} not found.");

        // Name only — the email stays whatever the account has (placeholder or real).
        user.UpdateProfile(command.FirstName.Trim(), command.LastName.Trim(), user.Email);

        var profile = await profiles.GetByUserIdAsync(command.UserId, ct);
        if (profile is null)
        {
            profile = EmployeeProfile.Create(currentUser.CompanyId, command.UserId);
            await profiles.AddAsync(profile, ct);
        }

        profile.UpdatePersonal(
            command.JobTitle?.Trim(), command.Phone, command.Gender, command.Birthday, command.Svnr,
            command.Nationality, command.Department, command.Location, command.Address,
            command.EmergencyName, command.EmergencyRelation, command.EmergencyPhone,
            command.EmploymentStart, command.EmploymentEnd);
        profile.UpdateCv(command.CvJson);
        profile.ReplaceAllowances(command.Allowances.Select(a =>
            (a.Name, new Money(a.Amount, a.Currency), a.Unit)));

        await UpsertEntitlementAsync(command, ct);
        await UpsertTermsAsync(command, ct);

        await profiles.SaveChangesAsync(ct);
    }

    private async Task UpsertEntitlementAsync(UpdateWorkerCommand command, CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var entitlement = await absences.GetEntitlementAsync(command.UserId, year, ct);
        if (entitlement is null)
        {
            await absences.AddEntitlementAsync(
                AbsenceEntitlement.Create(currentUser.CompanyId, command.UserId, year,
                    command.AnnualVacationDays, command.CarriedOverDays), ct);
        }
        else
        {
            // Days already used are preserved — adjusting the allowance never rewrites history.
            entitlement.SetEntitlement(command.AnnualVacationDays, command.CarriedOverDays);
        }
    }

    private async Task UpsertTermsAsync(UpdateWorkerCommand command, CancellationToken ct)
    {
        var day = command.DayHours;
        decimal? At(int i) => day != null && day[i] is > 0 ? day[i] : null;

        var workingDays = WeekDays.None;
        var flags = new[]
        {
            WeekDays.Monday, WeekDays.Tuesday, WeekDays.Wednesday, WeekDays.Thursday,
            WeekDays.Friday, WeekDays.Saturday, WeekDays.Sunday,
        };
        for (var i = 0; i < 7; i++)
            if (At(i) is not null)
                workingDays |= flags[i];

        var latest = await termsRepository.GetLatestAsync(command.UserId, ct);

        // Hourly workers have no schedule; keep a plausible weekly figure for target-hour math.
        var weekly = command.WorkModel == WorkModel.Hourly
            ? command.ExpectedWeeklyHours is > 0 ? command.ExpectedWeeklyHours.Value : latest?.WeeklyHours ?? 38.5m
            : Enumerable.Range(0, 7).Sum(i => At(i) ?? 0);
        if (workingDays == WeekDays.None)
            workingDays = latest?.WorkingDays ?? WeekDays.MondayToFriday;

        var isAllIn = command.OvertimeMode == OvertimeMode.AllIn;
        var hr = new HrTerms(
            command.WorkModel,
            At(0), At(1), At(2), At(3), At(4), At(5), At(6),
            command.OvertimeMode,
            command.OvertimeSurchargePct, command.NightSurchargePct,
            command.SundaySurchargePct, command.HolidaySurchargePct,
            command.HourlyRateAmount is > 0
                ? new Money(command.HourlyRateAmount.Value, command.HourlyRateCurrency ?? "EUR")
                : null,
            command.MaxHoursPerMonth, command.ExpectedWeeklyHours,
            command.TrackingRequired, command.AutoDeductBreak, command.Flexitime, command.OnCallDuty,
            command.CoreFrom, command.CoreTo,
            command.HomeOfficeDaysPerWeek, command.MaxHoursPerDay);

        // An all-in contract cannot also carry a lump sum (domain invariant).
        var lumpSum = isAllIn ? null : latest?.OvertimeLumpSumHours;

        var unchanged = latest is not null
            && latest.EmploymentType == command.EmploymentType
            && latest.WeeklyHours == weekly
            && latest.WorkingDays == workingDays
            && latest.AnnualVacationDays == command.AnnualVacationDays
            && latest.IsAllIn == isAllIn
            && latest.OvertimeLumpSumHours == lumpSum
            && latest.ToHrTerms() == hr;
        if (unchanged)
            return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var validFrom = latest is null ? command.EmploymentStart ?? today : Max(today, latest.ValidFrom);

        if (latest is not null && latest.ValidFrom == validFrom)
        {
            // Same-day correction: replace today's version instead of stacking a second one.
            // The predecessor was already closed at validFrom - 1, so ranges stay contiguous.
            await termsRepository.RemoveAsync(latest, ct);
        }
        else
        {
            latest?.CloseAt(validFrom);
        }

        var version = EmploymentTerms.Create(
            currentUser.CompanyId, command.UserId, validFrom,
            weekly, workingDays, command.AnnualVacationDays,
            command.EmploymentType, isAllIn, lumpSum, hr);

        await termsRepository.AddAsync(version, ct);
    }

    private static DateOnly Max(DateOnly a, DateOnly b) => a > b ? a : b;
}
