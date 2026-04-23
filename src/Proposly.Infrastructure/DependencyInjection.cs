using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.OfferManagement.Repositories;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Infrastructure.Persistence;
using Proposly.Infrastructure.Persistence.Repositories;
using Proposly.Infrastructure.Services;
using Proposly.Infrastructure.Services.Auth;
using Proposly.Infrastructure.Services.Email;
using Proposly.Infrastructure.Services.Pdf;
using QuestPDF.Infrastructure;

namespace Proposly.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IOfferRepository, OfferRepository>();
        services.AddScoped<IClientRepository, ClientRepository>();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddSingleton<IPdfService, OfferPdfService>();
        services.AddScoped<IEmailService, MailKitEmailService>();

        services.AddScoped<DataSeeder>();
        services.AddHostedService<OfferExpiryJob>();

        return services;
    }
}
