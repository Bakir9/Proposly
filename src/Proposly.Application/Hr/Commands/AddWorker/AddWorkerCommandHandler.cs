using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.HrManagement.Entities;
using Proposly.Domain.HrManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.Hr.Commands.AddWorker;

public sealed class AddWorkerCommandHandler(
    IUserRepository users,
    IEmployeeProfileRepository profiles,
    IEmploymentTermsRepository terms,
    IAbsenceRepository absences,
    ICurrentUserService currentUser) : ICommandHandler<AddWorkerCommand, Guid>
{
    public async Task<Guid> HandleAsync(AddWorkerCommand command, CancellationToken ct = default)
    {
        // Profile-only workers don't count toward the plan's user limit — the limit is enforced
        // when login access is granted (InviteWorker).
        var user = User.CreateProfileOnly(currentUser.CompanyId, command.FirstName.Trim(), command.LastName.Trim());
        await users.AddAsync(user, ct);

        var profile = EmployeeProfile.Create(currentUser.CompanyId, user.Id, command.JobTitle?.Trim());
        await profiles.AddAsync(profile, ct);

        // Sensible defaults so the Contract tab has data from the first second.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dailyDefault = Math.Round(38.5m / 5, 2);
        var initialTerms = EmploymentTerms.Create(
            currentUser.CompanyId, user.Id, today,
            weeklyHours: 38.5m,
            workingDays: WeekDays.MondayToFriday,
            annualVacationDays: 25,
            hr: new HrTerms(
                WorkModel: WorkModel.FullTime,
                MondayHours: dailyDefault, TuesdayHours: dailyDefault, WednesdayHours: dailyDefault,
                ThursdayHours: dailyDefault, FridayHours: dailyDefault));
        await terms.AddAsync(initialTerms, ct);

        // The current year's entitlement row exists from day one — absence approval consumes
        // from it, and a missing row would make the first vacation approval fail.
        await absences.AddEntitlementAsync(
            AbsenceEntitlement.Create(currentUser.CompanyId, user.Id, DateTime.UtcNow.Year,
                entitledDays: 25, carriedOverDays: 0), ct);

        await profiles.SaveChangesAsync(ct);
        return user.Id;
    }
}
