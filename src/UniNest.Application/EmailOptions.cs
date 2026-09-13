using System.ComponentModel.DataAnnotations;

namespace UniNest.Application;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    [Required(ErrorMessage = "Email:SmtpHost is required when email delivery is enabled.")]
    public string? SmtpHost { get; set; }

    public int SmtpPort { get; set; } = 587;

    public string? SmtpUser { get; set; }

    public string? SmtpPassword { get; set; }

    public bool UseSsl { get; set; } = true;

    [Required(ErrorMessage = "Email:FromAddress is required when email delivery is enabled.")]
    public string? FromAddress { get; set; }

    public string FromName { get; set; } = "UniNest";

    public string? BaseUrl { get; set; }

    public int ResetTokenExpiryMinutes { get; set; } = 60;
}
