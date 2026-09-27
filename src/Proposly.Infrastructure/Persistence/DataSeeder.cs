using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.ProjectManagement.Entities;
using Proposly.Domain.ProjectManagement.Enums;
using Proposly.Shared.Primitives;
using Proposly.Shared.ValueObjects;

namespace Proposly.Infrastructure.Persistence;

public sealed class DataSeeder
{
    private readonly IConfiguration _configuration;
    private readonly IPasswordHasher _passwordHasher;

    /// <summary>Fixed IDs so seed data is stable across re-runs.</summary>
    public static readonly Guid SeedCompanyId = Guid.Parse("a1b2c3d4-0000-0000-0000-000000000001");
    public static readonly Guid SeedUserId   = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public DataSeeder(IConfiguration configuration, IPasswordHasher passwordHasher)
    {
        _configuration = configuration;
        _passwordHasher = passwordHasher;
    }

    public async Task EnsureSuperAdminAsync(CancellationToken ct = default)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_configuration.GetConnectionString("DefaultConnection"))
            .Options;

        await using var context = new AppDbContext(options, new SeedCurrentUserService(), new NoOpDispatcher());

        var superAdminEmail = _configuration["Seed:SuperAdminEmail"] ?? "admin@proposly.io";
        var superAdminPassword = _configuration["Seed:SuperAdminPassword"] ?? "SuperAdmin123!";
        if (!await context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == superAdminEmail, ct))
        {
            var superAdmin = User.CreateSuperAdmin(
                superAdminEmail,
                _passwordHasher.Hash(superAdminPassword),
                "Super",
                "Admin");
            await context.Users.AddAsync(superAdmin, ct);
            await context.SaveChangesAsync(ct);
        }
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_configuration.GetConnectionString("DefaultConnection"))
            .Options;

        await using var context = new AppDbContext(options, new SeedCurrentUserService(), new NoOpDispatcher());

        if (await context.Companies.AnyAsync(ct))
            return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var company = Company.Create(SeedCompanyId, "Proposly Demo Company");

        var projects = new[]
        {
            BuildEcommerceProject(today),
            BuildMobileAppProject(today),
            BuildWebsiteProject(today)
        };

        await context.Companies.AddAsync(company, ct);
        await context.Projects.AddRangeAsync(projects, ct);
        await context.SaveChangesAsync(ct);
    }

    // -------------------------------------------------------------------------
    // Project 1 — E-Commerce Platform (Active, in progress)
    // -------------------------------------------------------------------------
    private static Project BuildEcommerceProject(DateOnly today)
    {
        var project = Project.Create(
            SeedCompanyId,
            "E-Commerce Platform",
            "Full-stack e-commerce solution with payment integration and inventory management.",
            Guid.Parse("b1000000-0000-0000-0000-000000000001"),
            "TechRetail GmbH",
            new Money(25_000, "EUR"),
            today.AddDays(-60),
            today.AddDays(30));

        project.Activate();

        var alice = project.AddMember(Guid.NewGuid(), "Alice Weber",    "Lead Developer",    new Money(95, "EUR"));
        var bob   = project.AddMember(Guid.NewGuid(), "Bob Müller",     "Backend Developer", new Money(80, "EUR"));
        var carol = project.AddMember(Guid.NewGuid(), "Carol Schneider","UX Designer",       new Money(75, "EUR"));

        var apiMilestone      = project.AddMilestone("Backend API Complete",   today.AddDays(-15));
        var frontendMilestone = project.AddMilestone("Frontend Integration",   today.AddDays(20));
        apiMilestone.Complete();

        var t1 = project.AddTask("Set up project structure",       null, 8,  today.AddDays(-65), today.AddDays(-55), apiMilestone.Id);
        var t2 = project.AddTask("Product catalog API",            null, 20, today.AddDays(-53), today.AddDays(-40), apiMilestone.Id);
        var t3 = project.AddTask("Payment gateway integration",    null, 16, today.AddDays(-38), today.AddDays(-20), apiMilestone.Id);
        var t4 = project.AddTask("Shopping cart frontend",         null, 24, today.AddDays(-14), today.AddDays(5),  frontendMilestone.Id);
        var t5 = project.AddTask("Checkout flow",                  null, 20, today.AddDays(6),  today.AddDays(20), frontendMilestone.Id);

        CompleteTask(t1); CompleteTask(t2); CompleteTask(t3);
        t4.Start();

        project.LogTime(alice.Id, 8,    "Project setup and architecture",    today.AddDays(-58));
        project.LogTime(alice.Id, 7.5m, "Database schema design",            today.AddDays(-52));
        project.LogTime(bob.Id,   8,    "Product catalog endpoints",         today.AddDays(-45));
        project.LogTime(bob.Id,   6,    "Payment gateway research",          today.AddDays(-30));
        project.LogTime(alice.Id, 8,    "Payment integration implementation",today.AddDays(-22));
        project.LogTime(carol.Id, 6,    "Wireframes and component library",  today.AddDays(-38));
        project.LogTime(carol.Id, 7,    "Shopping cart UI components",       today.AddDays(-10));
        project.LogTime(alice.Id, 5,    "Code review and refactoring",       today.AddDays(-5));

        project.AddExpense("Stripe payment gateway setup fee", new Money(150, "EUR"), ExpenseCategory.Other,      today.AddDays(-30));
        project.AddExpense("AWS S3 storage (3 months)",        new Money(45,  "EUR"), ExpenseCategory.Equipment,  today.AddDays(-15));
        project.AddExpense("Design assets license",            new Money(120, "EUR"), ExpenseCategory.Materials,  today.AddDays(-40));

        return project;
    }

    // -------------------------------------------------------------------------
    // Project 2 — Mobile App (Planning, not yet started)
    // -------------------------------------------------------------------------
    private static Project BuildMobileAppProject(DateOnly today)
    {
        var project = Project.Create(
            SeedCompanyId,
            "Mobile App Development",
            "Cross-platform mobile app for iOS and Android with real-time push notifications.",
            Guid.Parse("b1000000-0000-0000-0000-000000000002"),
            "StartupVision OG",
            new Money(40_000, "EUR"),
            today.AddDays(7),
            today.AddDays(120));

        project.AddMember(Guid.NewGuid(), "Dave Hofer", "Mobile Developer", new Money(90, "EUR"));
        project.AddMember(Guid.NewGuid(), "Eve Bauer",  "Product Manager",  new Money(85, "EUR"));

        project.AddMilestone("MVP Release",   today.AddDays(60));
        project.AddMilestone("Final Release", today.AddDays(115));

        project.AddTask("Requirements workshop",           null, 16, today.AddDays(7),  today.AddDays(8));
        project.AddTask("Technical architecture design",   null, 24, today.AddDays(9),  today.AddDays(20));
        project.AddTask("Authentication module",           null, 20, today.AddDays(21), today.AddDays(35));
        project.AddTask("Core features development",       null, 80, today.AddDays(36), today.AddDays(70));
        project.AddTask("QA and performance testing",      null, 24, today.AddDays(71), today.AddDays(100));

        return project;
    }

    // -------------------------------------------------------------------------
    // Project 3 — Corporate Website Redesign (Completed, linked to offer)
    // -------------------------------------------------------------------------
    private static Project BuildWebsiteProject(DateOnly today)
    {
        var project = Project.Create(
            SeedCompanyId,
            "Corporate Website Redesign",
            "Complete redesign of corporate website with headless CMS integration.",
            Guid.Parse("b1000000-0000-0000-0000-000000000003"),
            "GlobalCorp AG",
            new Money(8_000, "EUR"),
            today.AddDays(-90),
            today.AddDays(-15));

        project.LinkOffer(Guid.NewGuid(), new Money(9_500, "EUR")); // snapshot at offer-acceptance time
        project.Activate();

        var frank = project.AddMember(Guid.NewGuid(), "Frank Gruber", "Full Stack Developer", new Money(85, "EUR"));
        var greta = project.AddMember(Guid.NewGuid(), "Greta Leitner","Designer",             new Money(70, "EUR"));

        var designPhase = project.AddMilestone("Design Phase",      today.AddDays(-55));
        var devPhase    = project.AddMilestone("Development Phase",  today.AddDays(-20));
        designPhase.Complete();
        devPhase.Complete();

        var t1 = project.AddTask("Discovery and sitemap",    null, 8,  today.AddDays(-90), today.AddDays(-85), designPhase.Id);
        var t2 = project.AddTask("UI mockups and prototype", null, 20, today.AddDays(-84), today.AddDays(-70), designPhase.Id);
        var t3 = project.AddTask("CMS setup and templates",  null, 24, today.AddDays(-69), today.AddDays(-45), devPhase.Id);
        var t4 = project.AddTask("Content migration",        null, 16, today.AddDays(-44), today.AddDays(-30), devPhase.Id);
        var t5 = project.AddTask("Testing and deployment",   null, 12, today.AddDays(-29), today.AddDays(-16), devPhase.Id);

        CompleteTask(t1); CompleteTask(t2); CompleteTask(t3); CompleteTask(t4); CompleteTask(t5);

        project.LogTime(frank.Id, 8,    "Discovery and planning session",    today.AddDays(-88));
        project.LogTime(greta.Id, 7,    "Wireframing",                       today.AddDays(-80));
        project.LogTime(greta.Id, 8,    "UI design — homepage and subpages", today.AddDays(-72));
        project.LogTime(greta.Id, 6,    "Design revisions after feedback",   today.AddDays(-65));
        project.LogTime(frank.Id, 8,    "CMS installation and configuration",today.AddDays(-50));
        project.LogTime(frank.Id, 8,    "Page templates and components",     today.AddDays(-44));
        project.LogTime(frank.Id, 7,    "Content migration scripts",         today.AddDays(-35));
        project.LogTime(frank.Id, 6,    "Cross-browser testing",             today.AddDays(-18));

        project.AddExpense("Figma Pro subscription",   new Money(45,  "EUR"), ExpenseCategory.Equipment, today.AddDays(-85));
        project.AddExpense("Stock photography license",new Money(180, "EUR"), ExpenseCategory.Materials, today.AddDays(-60));
        project.AddExpense("Hosting setup (annual)",   new Money(120, "EUR"), ExpenseCategory.Equipment, today.AddDays(-17));

        project.Complete();

        return project;
    }

    private static void CompleteTask(ProjectTask task) { task.Start(); task.Complete(); }
}

file sealed class SeedCurrentUserService : ICurrentUserService
{
    public Guid CompanyId => DataSeeder.SeedCompanyId;
    public Guid UserId    => DataSeeder.SeedUserId;
    public string Role    => "Owner";

    // Consistent with Role: the seeder acts as an Owner, so it can read user-owned rows for
    // every employee in the seeded company.
    public bool CanViewAllEmployees => true;
}

file sealed class NoOpDispatcher : IDomainEventDispatcher
{
    public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken ct = default) => Task.CompletedTask;
}
