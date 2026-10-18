namespace NextGenCoaching.Shared.Requests;

public class UpdateEmailSettingsRequest
{
    public bool Enabled { get; set; }
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string? SmtpUser { get; set; }
    public bool UseStartTls { get; set; } = true;

    /// <summary>
    /// New SMTP password. Leave null or empty to keep the existing stored password unchanged.
    /// </summary>
    public string? SmtpPassword { get; set; }
}
