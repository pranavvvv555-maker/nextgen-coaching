using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using NextGenCoaching.Core.Entities;
using NextGenCoaching.Core.Persistence;
using NextGenCoaching.Shared.Constants;
using NextGenCoaching.Shared.Dtos;
using NextGenCoaching.Shared.Enums;
using NextGenCoaching.Shared.Requests;
using NextGenCoaching.Shared.Responses;

namespace NextGenCoaching.Api.Endpoints;

public static class AdminWebinarsEndpoints
{
    public static IEndpointRouteBuilder MapAdminWebinarsEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(ApiRoutes.Admin.Webinars)
            .WithTags("Admin · Webinars")
            .RequireAuthorization("RequireCounsellor");

        // GET /admin/webinars - paged
        group.MapGet("/", async (
            AppDbContext db,
            int page = 1,
            int pageSize = 25,
            string? status = null,
            CancellationToken ct = default) =>
        {
            var p = Math.Max(1, page);
            var s = Math.Clamp(pageSize, 1, 100);

            var query = db.Webinars.AsNoTracking().Include(w => w.Course).AsQueryable();
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<WebinarStatus>(status, true, out var st))
                query = query.Where(w => w.Status == st);

            var total = await query.CountAsync(ct);

            var rows = await query
                .OrderByDescending(w => w.ScheduledAtUtc)
                .Skip((p - 1) * s).Take(s)
                .Select(w => new AdminWebinarDto
                {
                    Id = w.Id,
                    Title = w.Title,
                    Description = w.Description,
                    CourseId = w.CourseId,
                    CourseTitle = w.Course != null ? w.Course.Title : null,
                    ScheduledAtUtc = w.ScheduledAtUtc,
                    DurationMinutes = w.DurationMinutes,
                    JoinUrl = w.JoinUrl,
                    RecordingUrl = w.RecordingUrl,
                    Capacity = w.Capacity,
                    Status = w.Status.ToString(),
                    BannerImageUrl = w.BannerImageUrl,
                    RegistrationsCount = w.Registrations.Count,
                    AttendedCount = w.Registrations.Count(r => r.Attended),
                    CreatedAtUtc = w.CreatedAtUtc
                })
                .ToListAsync(ct);

            return Results.Ok(ApiResponse<PagedResult<AdminWebinarDto>>.Ok(new PagedResult<AdminWebinarDto>
            {
                Items = rows,
                TotalCount = total,
                Page = p,
                PageSize = s
            }));
        }).WithName("AdminListWebinars");

        // GET /admin/webinars/{id} - with registrations
        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var w = await db.Webinars.AsNoTracking()
                .Include(w => w.Course)
                .FirstOrDefaultAsync(w => w.Id == id, ct);
            if (w is null) return Results.NotFound(ApiResponse<AdminWebinarDto>.Fail("Webinar not found"));

            var dto = new AdminWebinarDto
            {
                Id = w.Id,
                Title = w.Title,
                Description = w.Description,
                CourseId = w.CourseId,
                CourseTitle = w.Course?.Title,
                ScheduledAtUtc = w.ScheduledAtUtc,
                DurationMinutes = w.DurationMinutes,
                JoinUrl = w.JoinUrl,
                RecordingUrl = w.RecordingUrl,
                Capacity = w.Capacity,
                Status = w.Status.ToString(),
                BannerImageUrl = w.BannerImageUrl,
                CreatedAtUtc = w.CreatedAtUtc
            };
            return Results.Ok(ApiResponse<AdminWebinarDto>.Ok(dto));
        }).WithName("AdminGetWebinar");

        // GET /admin/webinars/{id}/registrations
        group.MapGet("/{id:guid}/registrations", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var rows = await db.WebinarRegistrations.AsNoTracking()
                .Include(r => r.Lead)
                .Where(r => r.WebinarId == id)
                .OrderBy(r => r.RegisteredAtUtc)
                .Select(r => new AdminWebinarRegistrationDto
                {
                    Id = r.Id,
                    LeadId = r.LeadId,
                    FullName = r.Lead.FullName,
                    Email = r.Lead.Email,
                    Phone = r.Lead.Phone,
                    City = r.Lead.City,
                    Attended = r.Attended,
                    RegisteredAtUtc = r.RegisteredAtUtc
                })
                .ToListAsync(ct);

            return Results.Ok(ApiResponse<IReadOnlyList<AdminWebinarRegistrationDto>>.Ok(rows));
        }).WithName("AdminListWebinarRegistrations");

        // POST /admin/webinars
        group.MapPost("/", async (UpsertWebinarRequest req, AppDbContext db, CancellationToken ct) =>
        {
            var w = new Webinar
            {
                Title = req.Title,
                Description = req.Description,
                CourseId = req.CourseId,
                ScheduledAtUtc = req.ScheduledAtUtc.ToUniversalTime(),
                DurationMinutes = req.DurationMinutes,
                JoinUrl = req.JoinUrl,
                Capacity = req.Capacity,
                BannerImageUrl = req.BannerImageUrl,
                Status = WebinarStatus.Scheduled
            };
            db.Webinars.Add(w);
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<Guid>.Ok(w.Id, "Webinar created"));
        }).WithName("AdminCreateWebinar");

        // PUT /admin/webinars/{id}
        group.MapPut("/{id:guid}", async (Guid id, UpsertWebinarRequest req, AppDbContext db, CancellationToken ct) =>
        {
            var w = await db.Webinars.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (w is null) return Results.NotFound(ApiResponse<object>.Fail("Webinar not found"));

            w.Title = req.Title;
            w.Description = req.Description;
            w.CourseId = req.CourseId;
            w.ScheduledAtUtc = req.ScheduledAtUtc.ToUniversalTime();
            w.DurationMinutes = req.DurationMinutes;
            w.JoinUrl = req.JoinUrl;
            w.Capacity = req.Capacity;
            w.BannerImageUrl = req.BannerImageUrl;

            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<Guid>.Ok(w.Id, "Webinar updated"));
        }).WithName("AdminUpdateWebinar");

        // DELETE (soft) /admin/webinars/{id}
        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var w = await db.Webinars.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (w is null) return Results.NotFound(ApiResponse<object>.Fail("Webinar not found"));
            db.Webinars.Remove(w);     // soft-delete via the audit interceptor
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<object>.Ok(new { id }, "Webinar deleted"));
        }).WithName("AdminDeleteWebinar");

        // POST /admin/webinars/{id}/registrations/{regId}/attended  - mark attended
        group.MapPost("/{id:guid}/registrations/{regId:guid}/attended", async (
            Guid id, Guid regId, AppDbContext db, CancellationToken ct) =>
        {
            var reg = await db.WebinarRegistrations.FirstOrDefaultAsync(r => r.Id == regId && r.WebinarId == id, ct);
            if (reg is null) return Results.NotFound(ApiResponse<object>.Fail("Registration not found"));
            reg.Attended = !reg.Attended;
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<object>.Ok(new { reg.Id, reg.Attended }));
        }).WithName("AdminToggleWebinarAttended");

        return routes;
    }
}
