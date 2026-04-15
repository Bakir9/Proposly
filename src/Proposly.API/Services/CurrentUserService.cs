using System.Security.Claims;
using Proposly.Application.Abstractions;

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

    private Guid GetGuidClaim(string claimType)
    {
        var value = _httpContextAccessor.HttpContext?.User?.FindFirstValue(claimType)
            ?? throw new UnauthorizedAccessException($"Claim '{claimType}' is missing.");

        return Guid.TryParse(value, out var id)
            ? id
            : throw new UnauthorizedAccessException($"Claim '{claimType}' is not a valid GUID.");
    }
}
