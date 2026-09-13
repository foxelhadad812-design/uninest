using Microsoft.Extensions.Logging;
using UniNest.Application;

namespace UniNest.Infrastructure;

/// <summary>
/// No-op email service used when SMTP is not configured.
/// Prevents emails from being sent in development / staging without SMTP.
/// Logs that email delivery is disabled — but NEVER logs the reset token.
/// </summary>
public sealed class NullEmailService : IEmailService
{
    private readonly ILogger<NullEmailService> _logger;

    public NullEmailService(ILogger<NullEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendPasswordResetEmailAsync(string email, string displayName, string resetUrl, CancellationToken cancellationToken = default)
    {
        // Do not log the resetUrl — it contains the raw reset token.
        _logger.LogInformation(
            "Email delivery is not configured (Email:SmtpHost is not set). " +
            "Password reset email for {Email} was NOT sent. " +
            "Configure Email:SmtpHost to enable email delivery.",
            email);

        return Task.CompletedTask;
    }
}
