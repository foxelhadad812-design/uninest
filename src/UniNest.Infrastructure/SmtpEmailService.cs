using System.Globalization;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using UniNest.Application;

namespace UniNest.Infrastructure;

/// <summary>
/// SMTP-based email service using MailKit.
/// Enabled only when Email:SmtpHost is configured (see DependencyInjection).
///
/// Security considerations:
/// - The reset token is part of the resetUrl passed in by the caller.
///   This service does NOT log the token.
/// - Credentials come from configuration (environment / user-secrets) — never hard-coded.
/// - The connection is disposed after each send to avoid socket leaks.
/// </summary>
public sealed class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailOptions> options, ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(
        string email,
        string displayName,
        string resetUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
            throw new ArgumentException("A valid email address is required.", nameof(email));

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress!));
        message.To.Add(new MailboxAddress(displayName, email));
        message.Subject = "UniNest - Password Reset";

        var htmlBody = BuildHtmlBody(displayName, resetUrl);
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            var secureSocketOptions = _options.UseSsl
                ? ((_options.SmtpPort == 465) ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls)
                : SecureSocketOptions.None;

            await client.ConnectAsync(_options.SmtpHost!, _options.SmtpPort, secureSocketOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_options.SmtpUser))
                await client.AuthenticateAsync(_options.SmtpUser, _options.SmtpPassword, cancellationToken);

            await client.SendAsync(message, cancellationToken);
            _logger.LogInformation("Password reset email sent to {Email}.", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send password reset email to {Email}. SmtpHost={SmtpHost}, Port={Port}.",
                email, _options.SmtpHost, _options.SmtpPort);
            throw;
        }
        finally
        {
            await client.DisconnectAsync(true, cancellationToken);
        }
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailboxAddress("", email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private string BuildHtmlBody(string displayName, string resetUrl)
    {
        var safeName = displayName
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");

        return string.Format(@"<!DOCTYPE html>
            <html>
            <head><meta charset=""utf-8""></head>
            <body style=""font-family: Arial, sans-serif; color: #333;"">
                <p>Hi {0},</p>
                <p>You requested a password reset for your UniNest account.</p>
                <p>
                    <a href=""{1}""
                       style=""display: inline-block; padding: 12px 24px; background: #2563eb; color: white;
                              text-decoration: none; border-radius: 6px; margin-top: 16px;"">
                        Reset Your Password
                    </a>
                </p>
                <p>If the button above does not work, copy and paste this link into your browser:</p>
                <p style=""word-break: break-all; color: #666;"">{1}</p>
                <p style=""margin-top: 24px; color: #999; font-size: 12px;"">
                    This link expires in {2} minutes.
                    If you did not request this, please ignore this email.
                </p>
            </body>
            </html>",
            safeName, resetUrl, _options.ResetTokenExpiryMinutes);
    }
}
