namespace NextGenCoaching.Core.Settings;

/// <summary>
/// SMTP configuration resolved at runtime from the <c>app_settings</c> table
/// (deliberately NOT from appsettings.json). Managed via the admin settings API
/// and cached in-process by <see cref="Services.Settings.AppSettingsService"/>.
/// </summary>
public class EmailSettings
{
    public bool Enabled { get; set; }
    public string SmtpHost { get; set; } = "localhost";
    public int SmtpPort { get; set; } = 587;
    public string FromEmail { get; set; } = "noreply@nextgencoaching.local";
    public string FromName { get; set; } = "NextGen Coaching";
    public string? SmtpUser { get; set; }
    public string? SmtpPassword { get; set; }
    public bool UseStartTls { get; set; } = true;

    /// <summary>
    /// The <c>app_settings</c> keys these fields map to. Centralised so the seeder,
    /// the settings service, and the admin endpoint all agree on the same strings.
    /// </summary>
    public static class Keys
    {
        public const string Enabled      = "Email:Enabled";
        public const string SmtpHost     = "Email:SmtpHost";
        public const string SmtpPort     = "Email:SmtpPort";
        public const string FromEmail    = "Email:FromEmail";
        public const string FromName     = "Email:FromName";
        public const string SmtpUser     = "Email:SmtpUser";
        public const string SmtpPassword = "Email:SmtpPassword";
        public const string UseStartTls  = "Email:UseStartTls";
    }
}
