using Proposly.Domain.ProjectManagement.Enums;

namespace Proposly.Application.ProjectManagement.Responses;

public record ProjectDetailResponse(
    Guid Id,
    string Name,
    string? Description,
    string ClientName,
    ProjectStatus Status,
    decimal BudgetAmount,
    string Currency,
    DateOnly StartDate,
    DateOnly? Deadline,
    Guid? LinkedOfferId,
    decimal? OfferedAmount,
    ProfitabilityResponse Profitability,
    IReadOnlyList<ProjectMemberResponse> Members);

public record ProfitabilityResponse(
    decimal LaborCost,
    decimal ExpensesTotal,
    decimal TotalCost,
    decimal Revenue,
    decimal Profit,
    string Currency);

public record ProjectMemberResponse(
    Guid Id,
    Guid UserId,
    string Name,
    string Role,
    decimal HourlyRate,
    string Currency);
