namespace UniNest.Application;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string displayName, string resetUrl, CancellationToken cancellationToken = default);
}
