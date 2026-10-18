using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NextGenCoaching.Core.Identity;
using NextGenCoaching.Core.Interfaces;
using NextGenCoaching.Core.Persistence;
using NextGenCoaching.Shared.Constants;
using NextGenCoaching.Shared.Dtos;
using NextGenCoaching.Shared.Requests;
using NextGenCoaching.Shared.Responses;

namespace NextGenCoaching.Api.Endpoints;

public static class AdminUsersEndpoints
{
    public static IEndpointRouteBuilder MapAdminUsersEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(ApiRoutes.Admin.Users)
            .WithTags("Admin · Users")
            .RequireAuthorization("RequireAdmin");

        // GET paged + filterable list of all users (any role)
        group.MapGet("/", async (
            UserManager<ApplicationUser> userMgr,
            AppDbContext db,
            CancellationToken ct,
            int Page = 1, int PageSize = 25, string? Role = null, string? Search = null, bool? ActiveOnly = null) =>
        {
            var q = new UserListQuery { Page = Page, PageSize = PageSize, Role = Role, Search = Search, ActiveOnly = ActiveOnly };
            var page = Math.Max(1, q.Page);
            var size = Math.Clamp(q.PageSize, 1, 100);
            var now = DateTimeOffset.UtcNow;

            var query = db.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                var s = q.Search.Trim();
                query = query.Where(u =>
                    EF.Functions.ILike(u.FullName, $"%{s}%") ||
                    (u.Email != null && EF.Functions.ILike(u.Email, $"%{s}%")) ||
                    (u.PhoneNumber != null && EF.Functions.ILike(u.PhoneNumber, $"%{s}%")));
            }

            if (!string.IsNullOrWhiteSpace(q.Role))
            {
                var idsInRole = (await userMgr.GetUsersInRoleAsync(q.Role)).Select(u => u.Id).ToList();
                query = query.Where(u => idsInRole.Contains(u.Id));
            }

            if (q.ActiveOnly == true)
                query = query.Where(u => u.LockoutEnd == null || u.LockoutEnd <= now);

            var total = await query.CountAsync(ct);

            var users = await query
                .OrderByDescending(u => u.CreatedAtUtc)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync(ct);

            var items = new List<AdminUserListItemDto>(users.Count);
            foreach (var u in users)
            {
                var roles = await userMgr.GetRolesAsync(u);
                items.Add(new AdminUserListItemDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    Phone = u.PhoneNumber,
                    City = u.City,
                    State = u.State,
                    Roles = roles.ToList(),
                    CreatedAtUtc = u.CreatedAtUtc,
                    LastLoginAtUtc = u.LastLoginAtUtc,
                    IsActive = u.LockoutEnd == null || u.LockoutEnd <= now
                });
            }

            return Results.Ok(ApiResponse<PagedResult<AdminUserListItemDto>>.Ok(new PagedResult<AdminUserListItemDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = size
            }));
        }).WithName("AdminUsersList");

        // Activate / deactivate a user (lockout toggle)
        group.MapPost("/{id:guid}/toggle-active", async (
            Guid id,
            UserManager<ApplicationUser> userMgr,
            ICurrentUserService currentUser,
            CancellationToken ct) =>
        {
            if (currentUser.UserId == id)
                return Results.BadRequest(ApiResponse<object>.Fail("You can't deactivate your own account."));

            var u = await userMgr.FindByIdAsync(id.ToString());
            if (u is null) return Results.NotFound(ApiResponse<object>.Fail("User not found"));

            var now = DateTimeOffset.UtcNow;
            var isActive = u.LockoutEnd == null || u.LockoutEnd <= now;

            if (isActive)
            {
                await userMgr.SetLockoutEnabledAsync(u, true);
                await userMgr.SetLockoutEndDateAsync(u, DateTimeOffset.MaxValue);
            }
            else
            {
                await userMgr.SetLockoutEndDateAsync(u, null);
            }

            return Results.Ok(ApiResponse<object>.Ok(new { id, active = !isActive },
                isActive ? "User deactivated." : "User reactivated."));
        }).WithName("AdminUsersToggleActive");

        // Admin sets a new password for a user (no current-password needed)
        group.MapPost("/{id:guid}/reset-password", async (
            Guid id,
            SetUserPasswordRequest req,
            UserManager<ApplicationUser> userMgr,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword.Length < 8)
                return Results.BadRequest(ApiResponse<object>.Fail("Password must be at least 8 characters."));

            var u = await userMgr.FindByIdAsync(id.ToString());
            if (u is null) return Results.NotFound(ApiResponse<object>.Fail("User not found"));

            var token = await userMgr.GeneratePasswordResetTokenAsync(u);
            var result = await userMgr.ResetPasswordAsync(u, token, req.NewPassword);
            if (!result.Succeeded)
                return Results.BadRequest(ApiResponse<object>.Fail(
                    "Could not set password.", result.Errors.Select(e => e.Description).ToList()));

            return Results.Ok(ApiResponse<object>.Ok(new { id }, "Password updated."));
        }).WithName("AdminUsersResetPassword");

        // Admin assigns / replaces a user's roles
        group.MapPost("/{id:guid}/roles", async (
            Guid id,
            SetUserRolesRequest req,
            UserManager<ApplicationUser> userMgr,
            ICurrentUserService currentUser,
            CancellationToken ct) =>
        {
            var u = await userMgr.FindByIdAsync(id.ToString());
            if (u is null) return Results.NotFound(ApiResponse<object>.Fail("User not found"));

            // keep only valid, known role names
            var requested = (req.Roles ?? new List<string>())
                .Where(r => RoleNames.All.Contains(r, StringComparer.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // an admin must not strip the Admin role from their own account
            if (currentUser.UserId == id &&
                !requested.Contains(RoleNames.Admin, StringComparer.OrdinalIgnoreCase))
                return Results.BadRequest(ApiResponse<object>.Fail(
                    "You can't remove the Admin role from your own account."));

            var current = await userMgr.GetRolesAsync(u);
            var toAdd = requested.Where(r => !current.Contains(r, StringComparer.OrdinalIgnoreCase)).ToList();
            var toRemove = current.Where(r => !requested.Contains(r, StringComparer.OrdinalIgnoreCase)).ToList();

            if (toRemove.Count > 0)
            {
                var rem = await userMgr.RemoveFromRolesAsync(u, toRemove);
                if (!rem.Succeeded)
                    return Results.BadRequest(ApiResponse<object>.Fail(
                        "Could not update roles.", rem.Errors.Select(e => e.Description).ToList()));
            }

            if (toAdd.Count > 0)
            {
                var add = await userMgr.AddToRolesAsync(u, toAdd);
                if (!add.Succeeded)
                    return Results.BadRequest(ApiResponse<object>.Fail(
                        "Could not update roles.", add.Errors.Select(e => e.Description).ToList()));
            }

            return Results.Ok(ApiResponse<object>.Ok(new { id, roles = requested }, "Roles updated."));
        }).WithName("AdminUsersSetRoles");

        return routes;
    }
}
