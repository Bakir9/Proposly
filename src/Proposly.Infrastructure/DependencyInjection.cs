using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Infrastructure.Persistence;
using Proposly.Infrastructure.Persistence.Repositories;

namespace Proposly.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<DataSeeder>();

        return services;
    }
}
