namespace NextGenCoaching.Core.Entities;

/// <summary>
/// Dedicated single-row table holding SMTP configuration. This replaces the previous
/// key-value storage in app_settings. Convention: exactly one row, with Id == SingletonId.
/// </summary>
public class EmailSetting
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;
    public bool Enabled { get; set; }
    public string SmtpHost { get; set; } = "localhost";
    public int SmtpPort { get; set; } = 587;
    public string FromEmail { get; set; } = "noreply@nextgencoaching.local";
    public string FromName { get; set; } = "NextGen Coaching";
    public string? SmtpUser { get; set; }
    public string? SmtpPassword { get; set; }
    public bool UseStartTls { get; set; } = true;
    public DateTime? UpdatedAtUtc { get; set; }
}
