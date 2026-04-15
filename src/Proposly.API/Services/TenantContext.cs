using Proposly.Application.Abstractions;

namespace Proposly.API.Services;

/// <summary>
/// Scoped service populated by TenantMiddleware on every authenticated request.
/// All other services read tenant info from here instead of raw JWT claims.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    public Guid CompanyId { get; private set; }
    public string CompanyName { get; private set; } = string.Empty;

    internal void Set(Guid companyId, string companyName)
    {
        CompanyId = companyId;
        CompanyName = companyName;
    }
}
