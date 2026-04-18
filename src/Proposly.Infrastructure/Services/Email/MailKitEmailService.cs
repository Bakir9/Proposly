using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Proposly.Application.Abstractions;

namespace Proposly.Infrastructure.Services.Email;

public sealed class MailKitEmailService : IEmailService
{
    private readonly SmtpSettings _settings;

    public MailKitEmailService(IConfiguration configuration)
    {
        var s = configuration.GetSection("Smtp");
        _settings = new SmtpSettings
        {
            Host = s["Host"] ?? "localhost",
            Port = int.TryParse(s["Port"], out var port) ? port : 1025,
            UseSsl = bool.TryParse(s["UseSsl"], out var ssl) && ssl,
            Username = s["Username"] ?? string.Empty,
            Password = s["Password"] ?? string.Empty,
            SenderEmail = s["SenderEmail"] ?? "noreply@proposly.app",
            SenderName = s["SenderName"] ?? "Proposly",
        };
    }

    public async Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        byte[]? attachmentBytes = null,
        string? attachmentFileName = null,
        CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var builder = new BodyBuilder { HtmlBody = htmlBody };

        if (attachmentBytes is not null && attachmentFileName is not null)
            builder.Attachments.Add(attachmentFileName, attachmentBytes, ContentType.Parse("application/pdf"));

        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port,
            _settings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable, ct);

        if (!string.IsNullOrEmpty(_settings.Username))
            await client.AuthenticateAsync(_settings.Username, _settings.Password, ct);

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}

internal sealed class SmtpSettings
{
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 1025;
    public bool UseSsl { get; init; } = false;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string SenderEmail { get; init; } = "noreply@proposly.app";
    public string SenderName { get; init; } = "Proposly";
}
