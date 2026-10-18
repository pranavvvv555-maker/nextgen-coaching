using NextGenCoaching.Core.Identity;

namespace NextGenCoaching.Core.Identity;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateAccessToken(ApplicationUser user, IReadOnlyList<string> roles);
    string CreateRefreshToken();
}
