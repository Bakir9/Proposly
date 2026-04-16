namespace Proposly.Application.Auth.Responses;

public record AuthResponse(
    string Token,
    Guid UserId,
    Guid CompanyId,
    string FullName,
    string Email,
    string Role);
