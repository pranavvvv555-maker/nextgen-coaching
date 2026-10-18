namespace NextGenCoaching.Shared.Dtos;

/// <summary>
/// Email/SMTP settings as shown to the admin. The password is never returned to the
/// client — <see cref="HasPassword"/> indicates whether one is stored.
/// </summary>
public class EmailSettingsDto
{
    public bool Enabled { get; set; }
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string? SmtpUser { get; set; }
    public bool UseStartTls { get; set; }
    public bool HasPassword { get; set; }
}
