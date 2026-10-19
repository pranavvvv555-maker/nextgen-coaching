using Microsoft.EntityFrameworkCore;
using NextGenCoaching.Core.Interfaces;
using NextGenCoaching.Core.Persistence;
using NextGenCoaching.Shared.Constants;

namespace NextGenCoaching.Api.Endpoints;

public static class ResumeFileEndpoints
{
    public static IEndpointRouteBuilder MapResumeFileEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/v1/files/resumes/{id:guid}", async (Guid id, ICurrentUserService user, AppDbContext db, IFileStorage storage, HttpContext http, CancellationToken ct) =>
        {
            var resume = await db.Resumes.AsNoTracking().Include(r => r.StudentProfile).FirstOrDefaultAsync(r => r.Id == id, ct);
            if (resume is null) return Results.NotFound();
            if (resume.StudentProfile.UserId != user.UserId && !user.IsInRole(RoleNames.Admin) && !user.IsInRole(RoleNames.HrReviewer))
                return Results.NotFound();
            try
            {
                var stream = await storage.DownloadAsync(resume.FileUrl, ct);
                http.Response.Headers.CacheControl = "no-store";
                return Results.File(stream, "application/octet-stream", $"resume-v{resume.Version}{Path.GetExtension(resume.FileUrl)}");
            }
            catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException or ArgumentException)
            {
                return Results.NotFound();
            }
        }).RequireAuthorization().WithName("DownloadResume");
        return routes;
    }
}
