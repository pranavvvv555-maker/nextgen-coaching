using Microsoft.EntityFrameworkCore;
using NextGenCoaching.Core.Entities;
using NextGenCoaching.Core.Enums;
using NextGenCoaching.Core.Persistence;
using NextGenCoaching.Shared.Constants;
using NextGenCoaching.Shared.Dtos;
using NextGenCoaching.Shared.Enums;
using NextGenCoaching.Shared.Requests;
using NextGenCoaching.Shared.Responses;

namespace NextGenCoaching.Api.Endpoints;

public static class AdminBatchesEndpoints
{
    public static IEndpointRouteBuilder MapAdminBatchesEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(ApiRoutes.Admin.Batches)
            .WithTags("Admin/Batches")
            .RequireAuthorization("RequireAdmin");

        // List
        group.MapGet("/", async (
            AppDbContext db,
            Guid? courseId = null,
            string? status = null,
            CancellationToken ct = default) =>
        {
            var q = db.Batches.AsNoTracking()
                .Include(b => b.Course)
                .Include(b => b.PrimaryInstructor).ThenInclude(p => p!.User)
                .Include(b => b.Enrollments)
                .AsQueryable();

            if (courseId is not null) q = q.Where(b => b.CourseId == courseId.Value);
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BatchStatus>(status, true, out var st))
                q = q.Where(b => b.Status == st);

            var rows = await q.OrderByDescending(b => b.StartDate)
                .Select(b => new AdminBatchListItemDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    CourseId = b.CourseId,
                    CourseTitle = b.Course.Title,
                    PrimaryInstructorId = b.PrimaryInstructorId,
                    PrimaryInstructorName = b.PrimaryInstructor != null && b.PrimaryInstructor.User != null
                        ? b.PrimaryInstructor.User.FullName : null,
                    StartDate = b.StartDate,
                    EndDate = b.EndDate,
                    Mode = b.Mode.ToString(),
                    ScheduleDescription = b.ScheduleDescription,
                    Capacity = b.Capacity,
                    EnrolledCount = b.Enrollments.Count,
                    Status = b.Status.ToString()
                })
                .ToListAsync(ct);

            return Results.Ok(ApiResponse<IReadOnlyList<AdminBatchListItemDto>>.Ok(rows));
        }).WithName("AdminListBatches");

        // Get one (for edit)
        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var b = await db.Batches.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (b is null) return Results.NotFound(ApiResponse<UpsertBatchRequest>.Fail("Batch not found"));

            return Results.Ok(ApiResponse<UpsertBatchRequest>.Ok(new UpsertBatchRequest
            {
                Name = b.Name,
                CourseId = b.CourseId,
                PrimaryInstructorId = b.PrimaryInstructorId,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                Mode = b.Mode.ToString(),
                ScheduleDescription = b.ScheduleDescription,
                Capacity = b.Capacity,
                MeetingRoomUrl = b.MeetingRoomUrl,
                Status = b.Status.ToString()
            }));
        }).WithName("AdminGetBatch");

        // Create
        group.MapPost("/", async (UpsertBatchRequest req, AppDbContext db, CancellationToken ct) =>
        {
            if (!await db.Courses.AnyAsync(c => c.Id == req.CourseId, ct))
                return Results.BadRequest(ApiResponse<object>.Fail("Course not found"));

            if (req.PrimaryInstructorId is not null
                && !await db.InstructorProfiles.AnyAsync(p => p.Id == req.PrimaryInstructorId.Value, ct))
                return Results.BadRequest(ApiResponse<object>.Fail("Instructor not found"));

            if (!Enum.TryParse<BatchMode>(req.Mode, true, out var mode))
                return Results.BadRequest(ApiResponse<object>.Fail($"Mode must be Weekend, Daily, or Hybrid (was '{req.Mode}')"));

            if (await db.Batches.AnyAsync(b => b.Name == req.Name, ct))
                return Results.Conflict(ApiResponse<object>.Fail($"A batch named '{req.Name}' already exists. Names should be unique."));

            var b = new Batch
            {
                Name = req.Name.Trim(),
                CourseId = req.CourseId,
                PrimaryInstructorId = req.PrimaryInstructorId,
                StartDate = req.StartDate,
                EndDate = req.EndDate,
                Mode = mode,
                ScheduleDescription = req.ScheduleDescription,
                Capacity = req.Capacity,
                MeetingRoomUrl = req.MeetingRoomUrl,
                Status = BatchStatus.Planned
            };
            db.Batches.Add(b);
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<Guid>.Ok(b.Id, "Batch created"));
        }).WithName("AdminCreateBatch");

        // Update
        group.MapPut("/{id:guid}", async (Guid id, UpsertBatchRequest req, AppDbContext db, CancellationToken ct) =>
        {
            var b = await db.Batches.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (b is null) return Results.NotFound(ApiResponse<object>.Fail("Batch not found"));

            if (!await db.Courses.AnyAsync(c => c.Id == req.CourseId, ct))
                return Results.BadRequest(ApiResponse<object>.Fail("Course not found"));

            if (req.PrimaryInstructorId is not null
                && !await db.InstructorProfiles.AnyAsync(p => p.Id == req.PrimaryInstructorId.Value, ct))
                return Results.BadRequest(ApiResponse<object>.Fail("Instructor not found"));

            if (!Enum.TryParse<BatchMode>(req.Mode, true, out var mode))
                return Results.BadRequest(ApiResponse<object>.Fail($"Mode must be Weekend, Daily, or Hybrid (was '{req.Mode}')"));

            if (req.Name != b.Name && await db.Batches.AnyAsync(x => x.Name == req.Name && x.Id != id, ct))
                return Results.Conflict(ApiResponse<object>.Fail($"A batch named '{req.Name}' already exists."));

            b.Name = req.Name.Trim();
            b.CourseId = req.CourseId;
            b.PrimaryInstructorId = req.PrimaryInstructorId;
            b.StartDate = req.StartDate;
            b.EndDate = req.EndDate;
            b.Mode = mode;
            b.ScheduleDescription = req.ScheduleDescription;
            b.Capacity = req.Capacity;
            b.MeetingRoomUrl = req.MeetingRoomUrl;

            if (!string.IsNullOrWhiteSpace(req.Status))
            {
                if (!Enum.TryParse<BatchStatus>(req.Status, true, out var status))
                    return Results.BadRequest(ApiResponse<object>.Fail($"Invalid status '{req.Status}'"));
                b.Status = status;
            }

            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<Guid>.Ok(b.Id, "Batch updated"));
        }).WithName("AdminUpdateBatch");

        // Status-only update — quick action from list page
        group.MapPost("/{id:guid}/status/{status}", async (Guid id, string status, AppDbContext db, CancellationToken ct) =>
        {
            var b = await db.Batches.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (b is null) return Results.NotFound(ApiResponse<object>.Fail("Batch not found"));
            if (!Enum.TryParse<BatchStatus>(status, true, out var newStatus))
                return Results.BadRequest(ApiResponse<object>.Fail($"Invalid status '{status}'"));
            b.Status = newStatus;
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<object>.Ok(new { id, status = newStatus.ToString() }));
        }).WithName("AdminUpdateBatchStatus");

        return routes;
    }
}
