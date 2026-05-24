using Microsoft.AspNetCore.Mvc;
using Proposly.Application.Abstractions;

namespace Proposly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DiagnosticsController : ControllerBase
{
    [HttpPost("email-test")]
    public async Task<IActionResult> TestEmail(
        [FromQuery] string to,
        [FromServices] IEmailService emailService,
        CancellationToken ct)
    {
        await emailService.SendAsync(
            to,
            "Proposly SMTP Test",
            "<p>If you received this, the SMTP configuration is working correctly.</p>",
            ct: ct);

        return Ok(new { message = $"Test email sent to {to}" });
    }
}
