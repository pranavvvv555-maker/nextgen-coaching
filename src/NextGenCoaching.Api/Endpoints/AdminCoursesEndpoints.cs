using Microsoft.EntityFrameworkCore;
using NextGenCoaching.Core.Entities;
using NextGenCoaching.Core.Persistence;
using NextGenCoaching.Shared.Constants;
using NextGenCoaching.Shared.Dtos;
using NextGenCoaching.Shared.Requests;
using NextGenCoaching.Shared.Responses;

namespace NextGenCoaching.Api.Endpoints;

public static class AdminCoursesEndpoints
{
    public static IEndpointRouteBuilder MapAdminCoursesEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(ApiRoutes.Admin.Courses)
            .WithTags("Admin/Courses")
            .RequireAuthorization("RequireAdmin");

        // List all courses (admin sees unpublished too)
        group.MapGet("/", async (AppDbContext db, CancellationToken ct) =>
        {
            var rows = await db.Courses.AsNoTracking()
                .Include(c => c.Batches).ThenInclude(b => b.Enrollments)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Title)
                .Select(c => new AdminCourseListItemDto
                {
                    Id = c.Id,
                    Slug = c.Slug,
                    Title = c.Title,
                    Tagline = c.Tagline,
                    Category = c.Category,
                    DurationMonths = c.DurationMonths,
                    Price = c.Price,
                    DiscountPrice = c.DiscountPrice,
                    Currency = c.Currency,
                    Level = c.Level,
                    Mode = c.Mode,
                    IsPublished = c.IsPublished,
                    IsFlagship = c.IsFlagship,
                    SortOrder = c.SortOrder,
                    ActiveBatchesCount = c.Batches.Count(b => b.Status == NextGenCoaching.Core.Enums.BatchStatus.Active),
                    EnrolledStudentsCount = c.Batches.Sum(b => b.Enrollments.Count)
                })
                .ToListAsync(ct);
            return Results.Ok(ApiResponse<IReadOnlyList<AdminCourseListItemDto>>.Ok(rows));
        }).WithName("AdminListCourses");

        // Get one (so the edit modal can populate with full field set, not just the list DTO)
        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var c = await db.Courses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (c is null) return Results.NotFound(ApiResponse<UpsertCourseRequest>.Fail("Course not found"));
            var dto = new UpsertCourseRequest
            {
                Slug = c.Slug,
                Title = c.Title,
                Tagline = c.Tagline,
                Description = c.Description,
                DurationMonths = c.DurationMonths,
                TotalHours = c.TotalHours,
                Price = c.Price,
                DiscountPrice = c.DiscountPrice,
                Currency = c.Currency,
                Level = c.Level,
                Mode = c.Mode,
                HeroImageUrl = c.HeroImageUrl,
                BrochureUrl = c.BrochureUrl,
                IsPublished = c.IsPublished,
                IsFlagship = c.IsFlagship,
                Category = c.Category,
                SeoTitle = c.SeoTitle,
                SeoDescription = c.SeoDescription,
                SortOrder = c.SortOrder
            };
            return Results.Ok(ApiResponse<UpsertCourseRequest>.Ok(dto));
        }).WithName("AdminGetCourse");

        // Create
        group.MapPost("/", async (UpsertCourseRequest req, AppDbContext db, CancellationToken ct) =>
        {
            if (await db.Courses.AnyAsync(c => c.Slug == req.Slug, ct))
                return Results.Conflict(ApiResponse<object>.Fail($"Slug '{req.Slug}' is already used by another course"));

            var c = new Course
            {
                Slug = req.Slug.Trim().ToLowerInvariant(),
                Title = req.Title.Trim(),
                Tagline = req.Tagline,
                Description = req.Description,
                DurationMonths = req.DurationMonths,
                TotalHours = req.TotalHours,
                Price = req.Price,
                DiscountPrice = req.DiscountPrice,
                Currency = req.Currency,
                Level = req.Level,
                Mode = req.Mode,
                HeroImageUrl = req.HeroImageUrl,
                BrochureUrl = req.BrochureUrl,
                IsPublished = req.IsPublished,
                IsFlagship = req.IsFlagship,
                Category = req.Category,
                SeoTitle = req.SeoTitle,
                SeoDescription = req.SeoDescription,
                SortOrder = req.SortOrder
            };
            db.Courses.Add(c);
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<Guid>.Ok(c.Id, "Course created"));
        }).WithName("AdminCreateCourse");

        // Update
        group.MapPut("/{id:guid}", async (Guid id, UpsertCourseRequest req, AppDbContext db, CancellationToken ct) =>
        {
            var c = await db.Courses.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (c is null) return Results.NotFound(ApiResponse<object>.Fail("Course not found"));

            var newSlug = req.Slug.Trim().ToLowerInvariant();
            if (newSlug != c.Slug && await db.Courses.AnyAsync(x => x.Slug == newSlug && x.Id != id, ct))
                return Results.Conflict(ApiResponse<object>.Fail($"Slug '{newSlug}' is already used by another course"));

            c.Slug = newSlug;
            c.Title = req.Title.Trim();
            c.Tagline = req.Tagline;
            c.Description = req.Description;
            c.DurationMonths = req.DurationMonths;
            c.TotalHours = req.TotalHours;
            c.Price = req.Price;
            c.DiscountPrice = req.DiscountPrice;
            c.Currency = req.Currency;
            c.Level = req.Level;
            c.Mode = req.Mode;
            c.HeroImageUrl = req.HeroImageUrl;
            c.BrochureUrl = req.BrochureUrl;
            c.IsPublished = req.IsPublished;
            c.IsFlagship = req.IsFlagship;
            c.Category = req.Category;
            c.SeoTitle = req.SeoTitle;
            c.SeoDescription = req.SeoDescription;
            c.SortOrder = req.SortOrder;

            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<Guid>.Ok(c.Id, "Course updated"));
        }).WithName("AdminUpdateCourse");

        // Toggle published
        group.MapPost("/{id:guid}/toggle-published", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var c = await db.Courses.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (c is null) return Results.NotFound(ApiResponse<object>.Fail("Course not found"));
            c.IsPublished = !c.IsPublished;
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<object>.Ok(new { id, isPublished = c.IsPublished },
                c.IsPublished ? "Published" : "Unpublished"));
        }).WithName("AdminToggleCoursePublished");

        // ----- lookup: just id + title (for batch form dropdown) -----
        group.MapGet("/lookup", async (AppDbContext db, CancellationToken ct) =>
        {
            var rows = await db.Courses.AsNoTracking()
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Title)
                .Select(c => new SelectOptionDto { Id = c.Id, Label = c.Title, Sublabel = c.Slug })
                .ToListAsync(ct);
            return Results.Ok(ApiResponse<IReadOnlyList<SelectOptionDto>>.Ok(rows));
        }).WithName("AdminCoursesLookup");

        return routes;
    }
}
