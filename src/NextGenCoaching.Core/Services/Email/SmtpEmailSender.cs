using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using NextGenCoaching.Core.Interfaces;

namespace NextGenCoaching.Core.Services.Email;

/// <summary>
/// MailKit-based email sender. SMTP configuration is resolved at send time from the
/// dedicated <c>email_settings</c> table via <see cref="IEmailSettingsService"/> — not from
/// appsettings.json — so an admin can change it at runtime without a redeploy.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly IEmailSettingsService _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IEmailSettingsService settings, ILogger<SmtpEmailSender> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, string? plainTextBody = null, CancellationToken ct = default)
    {
        var cfg = await _settings.GetAsync(ct);

        if (!cfg.Enabled)
        {
            _logger.LogInformation("[Email disabled] To: {Email} Subject: {Subject}", toEmail, subject);
            return;
        }

        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(cfg.FromName, cfg.FromEmail));
        msg.To.Add(MailboxAddress.Parse(toEmail));
        msg.Subject = subject;
        msg.Body = new BodyBuilder
        {
            HtmlBody = htmlBody,
            TextBody = plainTextBody ?? StripHtml(htmlBody)
        }.ToMessageBody();

        var secureOptions = cfg.UseStartTls
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.Auto;

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(cfg.SmtpHost, cfg.SmtpPort, secureOptions, ct);
        if (!string.IsNullOrEmpty(cfg.SmtpUser))
        {
            if (!smtp.IsSecure) throw new InvalidOperationException("SMTP authentication requires an encrypted connection.");
            var password = cfg.SmtpPassword ?? throw new InvalidOperationException("Configure the SMTP password before enabling email.");
            await smtp.AuthenticateAsync(cfg.SmtpUser, password, ct);
        }
        await smtp.SendAsync(msg, ct);
        await smtp.DisconnectAsync(true, ct);

        _logger.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
    }

    private static string StripHtml(string html) =>
        System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
}
