using Proposly.API.Authorization;
using Proposly.Domain.CompanyManagement.Enums;

namespace Proposly.API.Extensions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddProposlyAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.OwnerOnly, policy =>
                policy.RequireRole(UserRole.Owner.ToString()));

            options.AddPolicy(Policies.ManageUsers, policy =>
                policy.RequireRole(UserRole.Owner.ToString(), UserRole.Admin.ToString()));

            options.AddPolicy(Policies.ManageOffers, policy =>
                policy.RequireRole(UserRole.Owner.ToString(), UserRole.Admin.ToString()));

            options.AddPolicy(Policies.ManageClients, policy =>
                policy.RequireRole(UserRole.Owner.ToString(), UserRole.Admin.ToString()));

            options.AddPolicy(Policies.ManageProjects, policy =>
                policy.RequireRole(UserRole.Owner.ToString(), UserRole.Admin.ToString()));

            options.AddPolicy(Policies.SuperAdminOnly, policy =>
                policy.RequireRole(UserRole.SuperAdmin.ToString()));

            // Every employee records their own working time and absence. SuperAdmin is excluded
            // deliberately — it is a cross-tenant admin role with no company of its own.
            options.AddPolicy(Policies.RecordOwnWorkTime, policy =>
                policy.RequireRole(
                    UserRole.Owner.ToString(),
                    UserRole.Admin.ToString(),
                    UserRole.Member.ToString()));

            options.AddPolicy(Policies.ApproveWorkTime, policy =>
                policy.RequireRole(UserRole.Owner.ToString(), UserRole.Admin.ToString()));

            options.AddPolicy(Policies.ManageWorkTimeSettings, policy =>
                policy.RequireRole(UserRole.Owner.ToString(), UserRole.Admin.ToString()));

            options.AddPolicy(Policies.ViewAllWorkTime, policy =>
                policy.RequireRole(UserRole.Owner.ToString(), UserRole.Admin.ToString()));
        });

        return services;
    }
}
