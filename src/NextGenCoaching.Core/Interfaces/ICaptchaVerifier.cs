namespace NextGenCoaching.Core.Interfaces;

/// <summary>
/// Verifies a human-verification token from the front end. Implementations
/// are expected to be safe-by-default: when no secret is configured the
/// verifier returns true (disabled), so the app keeps working until keys
/// are supplied.
/// </summary>
public interface ICaptchaVerifier
{
    Task<bool> VerifyAsync(string? token, string? remoteIp = null, CancellationToken ct = default);
}
