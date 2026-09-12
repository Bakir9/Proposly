using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Proposly.Application.Abstractions;
using Proposly.Application.Reports.Services;
using Proposly.Domain.CalendarManagement.Repositories;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.Notifications;
using Proposly.Domain.OfferManagement.Repositories;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Repositories;
using Proposly.Infrastructure.Persistence;
using Proposly.Infrastructure.Persistence.Repositories;
using Proposly.Infrastructure.Services;
using Proposly.Infrastructure.Services.Auth;
using Proposly.Infrastructure.Services.Email;
using Proposly.Infrastructure.Services.Events;
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

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        // Injectable clock. Used by WorkTimeManagement, whose rules are almost entirely
        // date/time based (rest periods across midnight, averaging windows, year-end balance
        // carry) and need a controllable clock to be testable. Existing code keeps using
        // DateTime.UtcNow directly — not retrofitted.
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<ITerminRepository, TerminRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IOfferRepository, OfferRepository>();
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<ITimesheetRepository, TimesheetRepository>();
        services.AddScoped<IWorkTimePolicyRepository, WorkTimePolicyRepository>();
        services.AddScoped<IAbsenceRepository, AbsenceRepository>();
        services.AddScoped<IEmploymentTermsRepository, EmploymentTermsRepository>();
        services.AddScoped<INonWorkingDayRepository, NonWorkingDayRepository>();

        services.AddSingleton<IAppSettings, AppSettings>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddSingleton<IPdfService, OfferPdfService>();
        services.AddSingleton<IReportPdfService, QuarterlyReportPdfService>();
        services.AddScoped<IEmailService, MailKitEmailService>();

        services.AddScoped<DataSeeder>();
        services.AddHostedService<OfferExpiryJob>();

        return services;
    }
}
