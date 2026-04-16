using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.OfferManagement.Repositories;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Infrastructure.Persistence;
using Proposly.Infrastructure.Persistence.Repositories;
using Proposly.Infrastructure.Services.Auth;

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
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IOfferRepository, OfferRepository>();
        services.AddScoped<IClientRepository, ClientRepository>();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtService, JwtService>();

        services.AddScoped<DataSeeder>();

        return services;
    }
}
