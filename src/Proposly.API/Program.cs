using Proposly.Application;
using Proposly.Application.Abstractions;
using Proposly.API.Extensions;
using Proposly.API.Middleware;
using Proposly.API.Services;
using Proposly.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();

builder.Services.AddHttpContextAccessor();

// TenantContext registered as both the concrete type (for middleware to set it)
// and the interface (for ICurrentUserService and AppDbContext to read it)
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<TenantMiddleware>();

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddProposlyAuthorization();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

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
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();
