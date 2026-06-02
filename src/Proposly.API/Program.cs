using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Proposly.Application;
using Proposly.Application.Abstractions;
using Proposly.API.Extensions;
using Proposly.API.Middleware;
using Proposly.API.Services;
using Proposly.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:5173";
Console.WriteLine($"=== CORS ORIGIN BEING USED: {allowedOrigin} ===");
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();
builder.Services.AddHealthChecks();

builder.Services.AddHttpContextAccessor();

// TenantContext registered as both the concrete type (for middleware to set it)
// and the interface (for ICurrentUserService and AppDbContext to read it)
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<TenantMiddleware>();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
    options.RejectionStatusCode = 429;
});

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddProposlyAuthorization();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Proposly.Infrastructure.Persistence.AppDbContext>();
    await db.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<Proposly.Infrastructure.Persistence.DataSeeder>();
    await seeder.EnsureSuperAdminAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Proposly API")
               .AddPreferredSecuritySchemes("Bearer")
               .AddHttpAuthentication("Bearer", bearer =>
               {
                   bearer.Token = string.Empty;
               });
    });

    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<Proposly.Infrastructure.Persistence.DataSeeder>();
    await seeder.SeedAsync();
}

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        var ex = feature?.Error;
        context.Response.ContentType = "application/problem+json";

        if (ex is CommandValidationException validationEx)
        {
            context.Response.StatusCode = 422;
            await context.Response.WriteAsJsonAsync(new
            {
                status = 422,
                title = "Validation failed.",
                errors = validationEx.Errors
            });
            return;
        }

        context.Response.StatusCode = ex is InvalidOperationException or ArgumentException ? 400 : 500;
        await context.Response.WriteAsJsonAsync(new
        {
            status = context.Response.StatusCode,
            title = ex is InvalidOperationException or ArgumentException
                ? ex.Message
                : "An unexpected error occurred.",
            detail = ex?.Message
        });
    });
});

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
