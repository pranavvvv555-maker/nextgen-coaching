using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NextGenCoaching.Core.Identity;
using NextGenCoaching.Core.Persistence;
using NextGenCoaching.Core.Services.Email;
using NextGenCoaching.Shared.Constants;
using NextGenCoaching.Shared.Dtos;
using NextGenCoaching.Shared.Requests;
using NextGenCoaching.Shared.Responses;

namespace NextGenCoaching.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(ApiRoutes.Base + "/auth").WithTags("Auth");

        group.MapPost("/login", async (
            LoginRequest req,
            UserManager<ApplicationUser> userMgr,
            ITokenService tokens,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var user = await userMgr.FindByEmailAsync(req.Email.Trim());
            if (user is null || await userMgr.IsLockedOutAsync(user))
                return Results.Unauthorized();

            if (!await userMgr.CheckPasswordAsync(user, req.Password))
            {
                await userMgr.AccessFailedAsync(user);
                return Results.Unauthorized();
            }
            await userMgr.ResetAccessFailedCountAsync(user);

            var roles = await userMgr.GetRolesAsync(user);
            var (access, expires) = tokens.CreateAccessToken(user, roles.ToList());
            var refresh = tokens.CreateRefreshToken();

            user.RefreshToken = refresh;
            user.RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(14);
            user.LastLoginAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            return Results.Ok(ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
            {
                AccessToken = access,
                RefreshToken = refresh,
                ExpiresAtUtc = expires,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Roles = roles.ToArray()
            }));
        }).AllowAnonymous().WithName("Login").RequireRateLimiting("auth");

        // ============================================================
        // PUBLIC SIGN-UP — creates a User + StudentProfile + Lead (for counsellor visibility)
        // and returns a JWT so the user is logged in immediately.
        // ============================================================
        group.MapPost("/register", async (
            RegisterRequest req,
            UserManager<ApplicationUser> userMgr,
            ITokenService tokens,
            AppDbContext db,
            IEmailDispatcher dispatcher,
            CancellationToken ct) =>
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync<IResult>(async () =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync(ct);
                // 1. Email uniqueness — bail early with a friendly message rather than a generic Identity error
                if (await userMgr.FindByEmailAsync(req.Email.Trim()) is not null)
                    return Results.Conflict(ApiResponse<AuthResponseDto>.Fail("An account with that email already exists. Try signing in instead."));

                // 2. Create the AspNetUsers row
                var user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = req.Email.Trim(),
                    Email = req.Email.Trim(),
                    FullName = req.FullName.Trim(),
                    PhoneNumber = req.Phone.Trim(),
                    City = req.City,
                    EmailConfirmed = false   // signal for future email-verification flow; safe today because RequireConfirmedEmail=false
                };
                var create = await userMgr.CreateAsync(user, req.Password);
                if (!create.Succeeded)
                {
                    // Surface Identity's specific complaints (weak password, etc.) verbatim
                    var msg = string.Join(" ", create.Errors.Select(e => e.Description));
                    return Results.BadRequest(ApiResponse<AuthResponseDto>.Fail(msg));
                }
                await userMgr.AddToRoleAsync(user, NextGenCoaching.Shared.Constants.RoleNames.Student);

                // 3. Create a StudentProfile so /student/* pages can find them
                var profile = new NextGenCoaching.Core.Entities.StudentProfile
                {
                    UserId = user.Id,
                    Status = NextGenCoaching.Core.Enums.StudentStatus.Active
                };
                db.StudentProfiles.Add(profile);

                // 4. Mirror existing public POST convention: create a Lead row so counsellors see the signal.
                //    Lead stays in 'New' state until either a counsellor works it or the user pays for a course.
                Guid? courseInterestId = null;
                if (!string.IsNullOrWhiteSpace(req.InterestedInCourseSlug))
                {
                    var slug = req.InterestedInCourseSlug.Trim().ToLowerInvariant();
                    courseInterestId = await db.Courses.AsNoTracking()
                        .Where(c => c.Slug == slug)
                        .Select(c => (Guid?)c.Id)
                        .FirstOrDefaultAsync(ct);
                }
                db.Leads.Add(new NextGenCoaching.Core.Entities.Lead
                {
                    FullName = user.FullName,
                    Email = user.Email!,
                    Phone = user.PhoneNumber ?? string.Empty,
                    City = user.City,
                    Source = NextGenCoaching.Shared.Enums.LeadSource.SelfSignUp,
                    CourseInterestId = courseInterestId,
                    Status = NextGenCoaching.Shared.Enums.LeadStatus.New,
                    Notes = "Self-registered via the public sign-up form."
                });

                // 5. Issue tokens — same shape as /login, so the client just stores and proceeds
                var roles = await userMgr.GetRolesAsync(user);
                var (access, expires) = tokens.CreateAccessToken(user, roles.ToList());
                var refresh = tokens.CreateRefreshToken();

                user.RefreshToken = refresh;
                user.RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(14);
                user.LastLoginAtUtc = DateTime.UtcNow;

                await db.SaveChangesAsync(ct);

                // Welcome email (fire-and-safe: never blocks the response)
                await transaction.CommitAsync(ct);
                await dispatcher.SendAsync("account_welcome", user.Email, new Dictionary<string, string?>
                {
                    ["full_name"] = user.FullName
                }, ct);

                return Results.Ok(ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
                {
                    AccessToken = access,
                    RefreshToken = refresh,
                    ExpiresAtUtc = expires,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    Roles = roles.ToArray()
                }, "Account created"));
            });
        }).AllowAnonymous().WithName("Register").RequireRateLimiting("auth");

        group.MapPost("/refresh", async (
            RefreshTokenRequest req,
            UserManager<ApplicationUser> userMgr,
            ITokenService tokens,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var user = await db.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == req.RefreshToken, ct);

            if (user is null || user.RefreshTokenExpiresAtUtc is null || user.RefreshTokenExpiresAtUtc < DateTime.UtcNow || await userMgr.IsLockedOutAsync(user))
                return Results.Unauthorized();

            var roles = await userMgr.GetRolesAsync(user);
            var (access, expires) = tokens.CreateAccessToken(user, roles.ToList());
            var newRefresh = tokens.CreateRefreshToken();

            user.RefreshToken = newRefresh;
            user.RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(14);
            await db.SaveChangesAsync(ct);

            return Results.Ok(ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
            {
                AccessToken = access,
                RefreshToken = newRefresh,
                ExpiresAtUtc = expires,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Roles = roles.ToArray()
            }));
        }).AllowAnonymous().WithName("RefreshToken");

        group.MapPost("/logout", async (
            UserManager<ApplicationUser> userMgr,
            AppDbContext db,
            HttpContext http,
            CancellationToken ct) =>
        {
            var email = http.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                        ?? http.User.FindFirst("email")?.Value;
            if (string.IsNullOrEmpty(email)) return Results.NoContent();

            var user = await userMgr.FindByEmailAsync(email);
            if (user is not null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiresAtUtc = null;
                await db.SaveChangesAsync(ct);
            }
            return Results.NoContent();
        }).RequireAuthorization().WithName("Logout");

        // POST /auth/reset-password — uses the token issued by GeneratePasswordResetTokenAsync
        // (also reused for lead conversion: PaymentService returns the same token to admins).
        group.MapPost("/reset-password", async (
            ResetPasswordPayload req,
            UserManager<ApplicationUser> userMgr,
            CancellationToken ct) =>
        {
            if (req.UserId == Guid.Empty || string.IsNullOrWhiteSpace(req.Token) || string.IsNullOrWhiteSpace(req.NewPassword))
                return Results.BadRequest(ApiResponse<object>.Fail("UserId, token and newPassword are required"));

            var user = await userMgr.FindByIdAsync(req.UserId.ToString());
            if (user is null) return Results.BadRequest(ApiResponse<object>.Fail("Invalid link"));

            var result = await userMgr.ResetPasswordAsync(user, req.Token, req.NewPassword);
            if (!result.Succeeded)
                return Results.BadRequest(ApiResponse<object>.Fail(string.Join("; ", result.Errors.Select(e => e.Description))));

            user.RefreshToken = null;
            user.RefreshTokenExpiresAtUtc = null;
            await userMgr.UpdateAsync(user);

            // Also confirm email on first password set — they clearly received the link.
            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                await userMgr.UpdateAsync(user);
            }

            return Results.Ok(ApiResponse<object>.Ok(new { ok = true }, "Password set"));
        }).AllowAnonymous().WithName("ResetPassword").RequireRateLimiting("auth");

        return routes;
    }

    // Inline payload type — kept here because it's only used by /reset-password and isn't worth a Shared DTO yet
    private record ResetPasswordPayload(Guid UserId, string Token, string NewPassword);
}
