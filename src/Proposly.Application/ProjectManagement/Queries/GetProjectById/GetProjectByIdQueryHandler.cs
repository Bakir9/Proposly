using Proposly.Application.Abstractions;
using Proposly.Application.ProjectManagement.Responses;
using Proposly.Domain.ProjectManagement.Repositories;

namespace Proposly.Application.ProjectManagement.Queries.GetProjectById;

public sealed class GetProjectByIdQueryHandler : IQueryHandler<GetProjectByIdQuery, ProjectDetailResponse?>
{
    private readonly IProjectRepository _repository;

    public GetProjectByIdQueryHandler(IProjectRepository repository) => _repository = repository;

    public async Task<ProjectDetailResponse?> HandleAsync(GetProjectByIdQuery query, CancellationToken ct = default)
    {
        var project = await _repository.GetByIdAsync(query.ProjectId, ct);
        if (project is null) return null;

        var laborCost = project.CalculateLaborCost();
        var totalCost = project.CalculateTotalCost();
        var expensesTotal = totalCost - laborCost;
        var revenue = project.OfferedAmount ?? project.Budget;
        var profit = project.CalculateProfitability();

        return new ProjectDetailResponse(
            project.Id, project.Name, project.Description, project.ClientName, project.Status,
            project.Budget.Amount, project.Budget.Currency,
            project.StartDate, project.Deadline,
            project.LinkedOfferId, project.OfferedAmount?.Amount,
            new ProfitabilityResponse(laborCost.Amount, expensesTotal.Amount, totalCost.Amount, revenue.Amount, profit.Amount, project.Budget.Currency),
            project.Members.Select(m => new ProjectMemberResponse(m.Id, m.UserId, m.Name, m.Role, m.HourlyRate.Amount, m.HourlyRate.Currency)).ToList(),
            project.Tasks.Select(t => new ProjectTaskResponse(t.Id, t.Title, t.Description, t.Status, t.EstimatedHours, t.DueDate, t.MilestoneId)).ToList(),
            project.Milestones.Select(m => new MilestoneResponse(m.Id, m.Title, m.DueDate, m.IsCompleted)).ToList(),
            project.Expenses.Select(e => new ExpenseResponse(e.Id, e.Description, e.Amount.Amount, e.Amount.Currency, e.Category.ToString(), e.Date)).ToList(),
            project.TimeEntries.Select(te => new TimeEntryResponse(
                te.Id, te.MemberId,
                project.Members.FirstOrDefault(m => m.Id == te.MemberId)?.Name ?? "Unknown",
                te.HoursWorked, te.HourlyRateSnapshot.Amount, te.HourlyRateSnapshot.Currency,
                te.Description, te.Date,
                te.HoursWorked * te.HourlyRateSnapshot.Amount)).ToList());
    }
}
