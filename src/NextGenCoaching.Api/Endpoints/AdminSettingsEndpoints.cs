using NextGenCoaching.Core.Entities;
using NextGenCoaching.Core.Interfaces;
using NextGenCoaching.Core.Services.Email;
using NextGenCoaching.Shared.Constants;
using NextGenCoaching.Shared.Dtos;
using NextGenCoaching.Shared.Requests;
using NextGenCoaching.Shared.Responses;

namespace NextGenCoaching.Api.Endpoints;

public static class AdminSettingsEndpoints
{
    public static IEndpointRouteBuilder MapAdminSettingsEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(ApiRoutes.Admin.Settings)
            .WithTags("Admin · Settings")
            .RequireAuthorization("RequireAdmin");

        // GET /admin/settings/email — never returns the password, only whether one is set.
        group.MapGet("/email", async (IEmailSettingsService settings, CancellationToken ct) =>
        {
            var s = await settings.GetAsync(ct);
            var dto = new EmailSettingsDto
            {
                Enabled = s.Enabled,
                SmtpHost = s.SmtpHost,
                SmtpPort = s.SmtpPort,
                FromEmail = s.FromEmail,
                FromName = s.FromName,
                SmtpUser = s.SmtpUser,
                UseStartTls = s.UseStartTls,
                HasPassword = !string.IsNullOrEmpty(s.SmtpPassword)
            };
            return Results.Ok(ApiResponse<EmailSettingsDto>.Ok(dto));
        }).WithName("AdminGetEmailSettings");

        // PUT /admin/settings/email — persists to the email_settings table and invalidates the cache.
        group.MapPut("/email", async (UpdateEmailSettingsRequest req, IEmailSettingsService settings, CancellationToken ct) =>
        {
            var current = await settings.GetAsync(ct);

            var updated = new EmailSetting
            {
                Id = EmailSetting.SingletonId,
                Enabled = req.Enabled,
                SmtpHost = req.SmtpHost,
                SmtpPort = req.SmtpPort,
                FromEmail = req.FromEmail,
                FromName = req.FromName,
                SmtpUser = req.SmtpUser,
                UseStartTls = req.UseStartTls,
                // A blank password in the request means "leave the stored one as is".
                SmtpPassword = string.IsNullOrEmpty(req.SmtpPassword) ? current.SmtpPassword : req.SmtpPassword
            };

            await settings.SaveAsync(updated, ct);
            return Results.Ok(ApiResponse<string>.Ok("saved", "Email settings updated."));
        }).WithName("AdminUpdateEmailSettings");

        // POST /admin/settings/email/test?to=someone@example.com — verify the live config.
        group.MapPost("/email/test", async (string? to, IEmailSender email, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(to))
                return Results.BadRequest(ApiResponse<string>.Fail("Provide a 'to' email address."));

            const string html = "<p>This is a test email from NextGen Coaching. " +
                                 "If you received it, your SMTP settings are working.</p>";

            await email.SendAsync(to, "NextGen Coaching — test email", html,
                "This is a test email from NextGen Coaching. If you received it, your SMTP settings are working.", ct);

            return Results.Ok(ApiResponse<string>.Ok("dispatched",
                "Test email dispatched. If email is disabled, it was logged instead of sent."));
        }).WithName("AdminSendTestEmail");

        return routes;
    }
}
