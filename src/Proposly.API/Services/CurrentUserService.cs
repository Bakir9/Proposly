using System.Security.Claims;
using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Enums;

namespace Proposly.API.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITenantContext _tenantContext;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, ITenantContext tenantContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _tenantContext = tenantContext;
    }

    // CompanyId comes from the middleware-validated TenantContext, not raw claims
    public Guid CompanyId => _tenantContext.CompanyId;

    public Guid UserId => GetGuidClaim(ClaimTypes.NameIdentifier);

    public string Role =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role)
        ?? throw new UnauthorizedAccessException("Role claim is missing.");

    // Deliberately non-throwing: this is read inside the IUserOwnedEntity query filter, which can
    // be evaluated outside an HTTP request (seeding, background jobs). No context => false, the
    // most restrictive answer.
    public bool CanViewAllEmployees
    {
        get
        {
            var role = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);
            return role == nameof(UserRole.Owner)
                || role == nameof(UserRole.Admin)
                || role == nameof(UserRole.SuperAdmin);
        }
    }

    private Guid GetGuidClaim(string claimType)
    {
        var value = _httpContextAccessor.HttpContext?.User?.FindFirstValue(claimType)
            ?? throw new UnauthorizedAccessException($"Claim '{claimType}' is missing.");

        return Guid.TryParse(value, out var id)
            ? id
            : throw new UnauthorizedAccessException($"Claim '{claimType}' is not a valid GUID.");
    }
}
