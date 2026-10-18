using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NextGenCoaching.Core.Entities;
using NextGenCoaching.Core.Identity;
using NextGenCoaching.Core.Persistence;
using NextGenCoaching.Core.Services.Email;
using NextGenCoaching.Shared.Constants;
using NextGenCoaching.Shared.Dtos;
using NextGenCoaching.Shared.Requests;
using NextGenCoaching.Shared.Responses;

namespace NextGenCoaching.Api.Endpoints;

public static class AdminInstructorsEndpoints
{
    public static IEndpointRouteBuilder MapAdminInstructorsEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(ApiRoutes.Admin.Instructors)
            .WithTags("Admin/Instructors")
            .RequireAuthorization("RequireAdmin");

        // ---------- Lookup (for dropdowns) ----------
        group.MapGet("/lookup", async (AppDbContext db, CancellationToken ct) =>
        {
            var rows = await db.InstructorProfiles.AsNoTracking()
                .Include(p => p.User)
                .Where(p => p.User != null)
                .OrderBy(p => p.User!.FullName)
                .Select(p => new SelectOptionDto
                {
                    Id = p.Id,
                    Label = p.User!.FullName,
                    Sublabel = p.Headline
                })
                .ToListAsync(ct);
            return Results.Ok(ApiResponse<IReadOnlyList<SelectOptionDto>>.Ok(rows));
        }).WithName("AdminInstructorsLookup");

        // ---------- List ----------
        group.MapGet("/", async (AppDbContext db, CancellationToken ct) =>
        {
            var rows = await db.InstructorProfiles.AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.Batches)
                .Where(p => p.User != null)
                .OrderBy(p => p.User!.FullName)
                .Select(p => new AdminInstructorListItemDto
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    FullName = p.User!.FullName,
                    Email = p.User.Email ?? string.Empty,
                    Headline = p.Headline,
                    YearsExperience = p.YearsExperience,
                    IsPublic = p.IsPublic,
                    BatchCount = p.Batches.Count,
                    CreatedAtUtc = p.CreatedAtUtc
                })
                .ToListAsync(ct);
            return Results.Ok(ApiResponse<IReadOnlyList<AdminInstructorListItemDto>>.Ok(rows));
        }).WithName("AdminInstructorsList");

        // ---------- Detail ----------
        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var p = await db.InstructorProfiles.AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Batches).ThenInclude(b => b.Course)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (p is null) return Results.NotFound(ApiResponse<AdminInstructorDetailDto>.Fail("Instructor not found"));

            var dto = new AdminInstructorDetailDto
            {
                Id = p.Id,
                UserId = p.UserId,
                FullName = p.User?.FullName ?? string.Empty,
                Email = p.User?.Email ?? string.Empty,
                Phone = p.User?.PhoneNumber,
                City = p.User?.City,
                Headline = p.Headline,
                Bio = p.Bio,
                ExpertiseTags = p.ExpertiseTags,
                YearsExperience = p.YearsExperience,
                LinkedinUrl = p.LinkedinUrl,
                HourlyRate = p.HourlyRate,
                IsPublic = p.IsPublic,
                CreatedAtUtc = p.CreatedAtUtc,
                Batches = p.Batches.Select(b => new AdminInstructorBatchDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    CourseTitle = b.Course != null ? b.Course.Title : string.Empty,
                    Status = b.Status.ToString()
                }).ToList()
            };
            return Results.Ok(ApiResponse<AdminInstructorDetailDto>.Ok(dto));
        }).WithName("AdminInstructorDetail");

        // ---------- Create (provisions user + profile + role + set-password email) ----------
        group.MapPost("/", async (
            CreateInstructorRequest req,
            UserManager<ApplicationUser> userMgr,
            AppDbContext db,
            IEmailDispatcher dispatcher,
            HttpContext http,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.FullName))
                return Results.BadRequest(ApiResponse<Guid>.Fail("Full name and email are required."));

            if (await userMgr.FindByEmailAsync(req.Email) is not null)
                return Results.Conflict(ApiResponse<Guid>.Fail("An account with that email already exists."));

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = req.Email.Trim(),
                Email = req.Email.Trim(),
                FullName = req.FullName.Trim(),
                PhoneNumber = req.Phone?.Trim(),
                City = req.City,
                EmailConfirmed = false
            };

            // Create with no password — the instructor sets it via the emailed link.
            var create = await userMgr.CreateAsync(user);
            if (!create.Succeeded)
                return Results.BadRequest(ApiResponse<Guid>.Fail(string.Join(" ", create.Errors.Select(e => e.Description))));

            await userMgr.AddToRoleAsync(user, RoleNames.Instructor);

            var profile = new InstructorProfile
            {
                UserId = user.Id,
                Headline = req.Headline,
                Bio = req.Bio,
                ExpertiseTags = req.ExpertiseTags ?? new(),
                YearsExperience = req.YearsExperience,
                LinkedinUrl = req.LinkedinUrl,
                HourlyRate = req.HourlyRate,
                IsPublic = req.IsPublic
            };
            db.InstructorProfiles.Add(profile);
            await db.SaveChangesAsync(ct);

            // Set-password email (fire-and-safe).
            var token = await userMgr.GeneratePasswordResetTokenAsync(user);
            var origin = http.Request.Headers.Origin.ToString();
            var encoded = Uri.EscapeDataString(token);
            var setUrl = string.IsNullOrEmpty(origin)
                ? $"/set-password?userId={user.Id}&token={encoded}"
                : $"{origin.TrimEnd('/')}/set-password?userId={user.Id}&token={encoded}";

            await dispatcher.SendAsync("set_password", user.Email, new Dictionary<string, string?>
            {
                ["full_name"] = user.FullName,
                ["set_password_url"] = setUrl
            }, ct);

            return Results.Ok(ApiResponse<Guid>.Ok(profile.Id, "Instructor created. A set-password email has been sent."));
        }).WithName("AdminCreateInstructor");

        // ---------- Update profile ----------
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateInstructorRequest req,
            UserManager<ApplicationUser> userMgr,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var p = await db.InstructorProfiles.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id, ct);
            if (p is null) return Results.NotFound(ApiResponse<object>.Fail("Instructor not found"));

            p.Headline = req.Headline;
            p.Bio = req.Bio;
            p.ExpertiseTags = req.ExpertiseTags ?? new();
            p.YearsExperience = req.YearsExperience;
            p.LinkedinUrl = req.LinkedinUrl;
            p.HourlyRate = req.HourlyRate;
            p.IsPublic = req.IsPublic;

            if (p.User is not null)
            {
                p.User.FullName = string.IsNullOrWhiteSpace(req.FullName) ? p.User.FullName : req.FullName.Trim();
                p.User.PhoneNumber = req.Phone?.Trim();
                p.User.City = req.City;
            }

            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<object>.Ok(new { p.Id }, "Instructor updated."));
        }).WithName("AdminUpdateInstructor");

        return routes;
    }
}
