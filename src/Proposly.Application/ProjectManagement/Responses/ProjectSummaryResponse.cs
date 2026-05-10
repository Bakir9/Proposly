using Proposly.Domain.ProjectManagement.Enums;

namespace Proposly.Application.ProjectManagement.Responses;

public record ProjectSummaryResponse(
    Guid Id, string Name, string ClientName, ProjectStatus Status,
    decimal BudgetAmount, string Currency, DateOnly StartDate, DateOnly? Deadline,
    DateTime CreatedAt, int MemberCount, int CompletedTasksCount, int TotalTasksCount);
