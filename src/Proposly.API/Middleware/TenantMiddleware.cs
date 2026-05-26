using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using Proposly.API.Services;
using Proposly.Domain.CompanyManagement.Enums;
using Proposly.Domain.CompanyManagement.Repositories;

namespace Proposly.API.Middleware;

public sealed class TenantMiddleware : IMiddleware
{
    private readonly ICompanyRepository _companyRepository;
    private readonly TenantContext _tenantContext;
    private readonly IMemoryCache _cache;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public TenantMiddleware(
        ICompanyRepository companyRepository,
        TenantContext tenantContext,
        IMemoryCache cache)
    {
        _companyRepository = companyRepository;
        _tenantContext = tenantContext;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        // SuperAdmin is cross-tenant — skip company validation entirely
        var role = context.User.FindFirstValue(ClaimTypes.Role);
        if (role == nameof(UserRole.SuperAdmin))
        {
            await next(context);
            return;
        }

        var companyIdClaim = context.User.FindFirstValue("company_id");

        if (!Guid.TryParse(companyIdClaim, out var companyId))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        var cacheKey = $"tenant:{companyId}";

        if (!_cache.TryGetValue(cacheKey, out CachedTenant? cached))
        {
            var company = await _companyRepository.GetByIdAsync(companyId, context.RequestAborted);

            if (company is null)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            cached = new CachedTenant(company.Id, company.Name, company.Status);

            _cache.Set(cacheKey, cached, new MemoryCacheEntryOptions
            {
                SlidingExpiration = CacheDuration
            });
        }

        if (cached!.Status != CompanyStatus.Active)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        _tenantContext.Set(cached.CompanyId, cached.Name);

        await next(context);
    }
}

file sealed record CachedTenant(Guid CompanyId, string Name, CompanyStatus Status);
