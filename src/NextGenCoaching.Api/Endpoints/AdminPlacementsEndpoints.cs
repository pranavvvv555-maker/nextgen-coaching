using Microsoft.EntityFrameworkCore;
using NextGenCoaching.Core.Persistence;
using NextGenCoaching.Shared.Constants;
using NextGenCoaching.Shared.Dtos;
using NextGenCoaching.Shared.Responses;

namespace NextGenCoaching.Api.Endpoints;

public static class AdminPlacementsEndpoints
{
    public static IEndpointRouteBuilder MapAdminPlacementsEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(ApiRoutes.Admin.Placements)
            .WithTags("Admin · Placements")
            .RequireAuthorization("RequireCounsellor");

        group.MapGet("/", async (string? search, AppDbContext db, CancellationToken ct) =>
        {
            var query = db.Placements.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(p =>
                    EF.Functions.ILike(p.CompanyName, $"%{s}%") ||
                    EF.Functions.ILike(p.RoleTitle, $"%{s}%") ||
                    (p.StudentProfile.User != null && EF.Functions.ILike(p.StudentProfile.User.FullName, $"%{s}%")));
            }

            var items = await query
                .OrderByDescending(p => p.PlacementDate)
                .Select(p => new AdminPlacementListItemDto
                {
                    Id = p.Id,
                    StudentProfileId = p.StudentProfileId,
                    StudentName = p.StudentProfile.User != null ? p.StudentProfile.User.FullName : "",
                    CompanyName = p.CompanyName,
                    RoleTitle = p.RoleTitle,
                    CourseTitle = p.Course.Title,
                    CtcLpa = p.CtcLpa,
                    PlacementDate = p.PlacementDate,
                    IsFeatured = p.IsFeatured
                })
                .ToListAsync(ct);

            return Results.Ok(ApiResponse<IReadOnlyList<AdminPlacementListItemDto>>.Ok(items));
        }).WithName("AdminListPlacements");

        return routes;
    }
}
