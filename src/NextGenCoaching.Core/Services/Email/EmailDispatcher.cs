using Microsoft.Extensions.Logging;

namespace NextGenCoaching.Core.Services.Email;

/// <summary>
/// Renders a template by key and sends it. Deliberately never throws — a failed or
/// disabled email must not break the user-facing flow that triggered it. Errors are logged.
/// </summary>
public interface IEmailDispatcher
{
    Task SendAsync(string templateKey, string? toEmail, IReadOnlyDictionary<string, string?> tokens, CancellationToken ct = default);
}

public sealed class EmailDispatcher : IEmailDispatcher
{
    private readonly ITemplateRenderer _renderer;
    private readonly Interfaces.IEmailSender _sender;
    private readonly ILogger<EmailDispatcher> _log;

    public EmailDispatcher(ITemplateRenderer renderer, Interfaces.IEmailSender sender, ILogger<EmailDispatcher> log)
    {
        _renderer = renderer;
        _sender = sender;
        _log = log;
    }

    public async Task SendAsync(string templateKey, string? toEmail, IReadOnlyDictionary<string, string?> tokens, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail)) return;

        try
        {
            var rendered = await _renderer.RenderAsync(templateKey, tokens, ct);
            if (rendered is null)
            {
                _log.LogWarning("Email template '{Key}' not found; skipping send to {Email}", templateKey, toEmail);
                return;
            }

            await _sender.SendAsync(toEmail, rendered.Subject, rendered.Html, rendered.Text, ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to dispatch '{Key}' email to {Email}", templateKey, toEmail);
        }
    }
}
