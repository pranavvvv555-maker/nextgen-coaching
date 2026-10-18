using Microsoft.Extensions.Logging;
using NextGenCoaching.Core.Interfaces;

namespace NextGenCoaching.Core.Services.Sms;

public class StubSmsSender : ISmsSender
{
    private readonly ILogger<StubSmsSender> _logger;
    public StubSmsSender(ILogger<StubSmsSender> logger) => _logger = logger;

    public Task SendAsync(string toPhone, string message, string? templateId = null, CancellationToken ct = default)
    {
        _logger.LogInformation("[SMS stub] To: {Phone} | Template: {TemplateId} | Body: {Body}", toPhone, templateId, message);
        return Task.CompletedTask;
    }
}
