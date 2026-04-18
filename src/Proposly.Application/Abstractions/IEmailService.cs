namespace Proposly.Application.Abstractions;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, byte[]? attachmentBytes = null, string? attachmentFileName = null, CancellationToken ct = default);
}
