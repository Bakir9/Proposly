using Proposly.Application.Abstractions;
using Proposly.Application.Hr.Responses;
using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.HrManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.Hr.Queries.GetWorkers;

public sealed class GetWorkersQueryHandler(
    IUserRepository users,
    IEmployeeProfileRepository profiles,
    IEmploymentTermsRepository terms) : IQueryHandler<GetWorkersQuery, IReadOnlyList<WorkerListItemResponse>>
{
    public async Task<IReadOnlyList<WorkerListItemResponse>> HandleAsync(
        GetWorkersQuery query, CancellationToken ct = default)
    {
        var allUsers = await users.GetAllAsync(ct);
        var allProfiles = (await profiles.GetAllAsync(ct)).ToDictionary(p => p.UserId);
        var latestTerms = (await terms.GetLatestForCompanyAsync(ct)).ToDictionary(t => t.UserId);

        return allUsers
            .Select(u =>
            {
                allProfiles.TryGetValue(u.Id, out var profile);
                latestTerms.TryGetValue(u.Id, out var t);
                return new WorkerListItemResponse(
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    profile?.JobTitle,
                    (t?.EmploymentType ?? Domain.WorkTimeManagement.Enums.EmploymentType.FullTime).ToString(),
                    (t?.WorkModel ?? Domain.WorkTimeManagement.Enums.WorkModel.FullTime).ToString(),
                    t?.WeeklyHours ?? 0,
                    AccountStatusOf(u),
                    profile?.PhotoStorageKey is not null);
            })
            .OrderBy(w => w.FirstName)
            .ToList();
    }

    internal static string AccountStatusOf(User u) =>
        u.IsDisabled ? "Disabled"
        : u.IsProfileOnly ? "ProfileOnly"
        : u.IsPendingInvite ? "Invited"
        : "Active";
}
