using Microsoft.Extensions.Configuration;
using Proposly.Application.Abstractions;

namespace Proposly.Infrastructure.Services;

public sealed class AppSettings : IAppSettings
{
    public string AppUrl { get; }

    public AppSettings(IConfiguration configuration)
    {
        AppUrl = configuration["AppUrl"] ?? "http://localhost:5173";
    }
}
