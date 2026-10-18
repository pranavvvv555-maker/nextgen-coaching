using Microsoft.EntityFrameworkCore;
using NextGenCoaching.Core.Persistence;
using NextGenCoaching.Shared.Constants;
using NextGenCoaching.Shared.Dtos;
using NextGenCoaching.Shared.Responses;

namespace NextGenCoaching.Api.Endpoints;

public static class AdminSearchEndpoints
{
    public static IEndpointRouteBuilder MapAdminSearchEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(ApiRoutes.Admin.Search)
            .WithTags("Admin · Search")
            .RequireAuthorization("RequireCounsellor");

        group.MapGet("/", async (string? q, AppDbContext db, CancellationToken ct) =>
        {
            var hits = new List<SearchHitDto>();
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
                return Results.Ok(ApiResponse<SearchResultsDto>.Ok(new SearchResultsDto { Query = q?.Trim() ?? string.Empty }));

            var s = q.Trim();
            var like = $"%{s}%";

            var students = await db.StudentProfiles.AsNoTracking().Include(p => p.User)
                .Where(p => p.User != null && (EF.Functions.ILike(p.User.FullName, like) || (p.User.Email != null && EF.Functions.ILike(p.User.Email, like))))
                .Take(5).ToListAsync(ct);
            hits.AddRange(students.Select(p => new SearchHitDto { Type = "Student", Id = p.Id, Title = p.User!.FullName, Subtitle = p.User.Email, Url = $"/admin/students/{p.Id}" }));

            var courses = await db.Courses.AsNoTracking()
                .Where(c => EF.Functions.ILike(c.Title, like) || EF.Functions.ILike(c.Slug, like))
                .Take(5).ToListAsync(ct);
            hits.AddRange(courses.Select(c => new SearchHitDto { Type = "Course", Id = c.Id, Title = c.Title, Subtitle = c.Slug, Url = "/admin/courses" }));

            var instructors = await db.InstructorProfiles.AsNoTracking().Include(p => p.User)
                .Where(p => p.User != null && EF.Functions.ILike(p.User.FullName, like))
                .Take(5).ToListAsync(ct);
            hits.AddRange(instructors.Select(p => new SearchHitDto { Type = "Instructor", Id = p.Id, Title = p.User!.FullName, Subtitle = p.Headline, Url = $"/admin/instructors/{p.Id}" }));

            var jobs = await db.JobOpenings.AsNoTracking()
                .Where(j => EF.Functions.ILike(j.CompanyName, like) || EF.Functions.ILike(j.RoleTitle, like))
                .Take(5).ToListAsync(ct);
            hits.AddRange(jobs.Select(j => new SearchHitDto { Type = "Job", Id = j.Id, Title = j.RoleTitle, Subtitle = j.CompanyName, Url = "/admin/placements" }));

            return Results.Ok(ApiResponse<SearchResultsDto>.Ok(new SearchResultsDto { Query = s, Hits = hits }));
        }).WithName("AdminSearch");

        return routes;
    }
}
