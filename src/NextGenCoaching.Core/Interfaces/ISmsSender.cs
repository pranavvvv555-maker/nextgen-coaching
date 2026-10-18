namespace NextGenCoaching.Core.Interfaces;

public interface ISmsSender
{
    Task SendAsync(string toPhone, string message, string? templateId = null, CancellationToken ct = default);
}
