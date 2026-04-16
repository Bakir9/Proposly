namespace Proposly.Application.UserManagement.Responses;

public record UserSummaryResponse(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    DateTime CreatedAt);

public record UserDetailResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string Role,
    Guid CompanyId,
    DateTime CreatedAt);
