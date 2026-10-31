using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Enums;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Shared.Exceptions;

namespace Proposly.Application.Admin.Commands.AdminUpdateCompanyPlan;

public sealed class AdminUpdateCompanyPlanCommandHandler : ICommandHandler<AdminUpdateCompanyPlanCommand>
{
    private readonly ICompanyRepository _companyRepository;

    public AdminUpdateCompanyPlanCommandHandler(ICompanyRepository companyRepository)
        => _companyRepository = companyRepository;

    public async Task HandleAsync(AdminUpdateCompanyPlanCommand command, CancellationToken ct = default)
    {
        var company = await _companyRepository.GetByIdAsync(command.CompanyId, ct)
            ?? throw AppException.NotFound("company.not_found", $"Company {command.CompanyId} not found.");

        var tier = Enum.Parse<PlanTier>(command.PlanTier, ignoreCase: true);
        company.SetPlan(tier, command.MaxUsers, command.MaxProjects, command.PlanExpiresAt);
        await _companyRepository.UpdateAsync(company, ct);
    }
}
