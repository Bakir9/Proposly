using Proposly.Application.Abstractions;
using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Commands.CreateEmploymentTerms;

public sealed class CreateEmploymentTermsCommandHandler
    : ICommandHandler<CreateEmploymentTermsCommand, Guid>
{
    private readonly IEmploymentTermsRepository _terms;
    private readonly ICurrentUserService _currentUser;

    public CreateEmploymentTermsCommandHandler(
        IEmploymentTermsRepository terms,
        ICurrentUserService currentUser)
    {
        _terms = terms;
        _currentUser = currentUser;
    }

    public async Task<Guid> HandleAsync(
        CreateEmploymentTermsCommand command, CancellationToken ct = default)
    {
        var latest = await _terms.GetLatestAsync(command.UserId, ct);

        if (latest is not null && command.ValidFrom <= latest.ValidFrom)
            throw new InvalidOperationException(
                $"New employment terms must take effect after {latest.ValidFrom:yyyy-MM-dd}.");

        var terms = EmploymentTerms.Create(
            _currentUser.CompanyId,
            command.UserId,
            command.ValidFrom,
            command.WeeklyHours,
            command.WorkingDays,
            command.AnnualVacationDays);

        // Closing the predecessor keeps version ranges contiguous and non-overlapping, so
        // "which terms applied in March" stays a single range predicate.
        latest?.CloseAt(command.ValidFrom);

        await _terms.AddAsync(terms, ct);

        return terms.Id;
    }
}
