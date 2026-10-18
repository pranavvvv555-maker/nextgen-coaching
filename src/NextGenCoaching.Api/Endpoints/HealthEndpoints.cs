using Microsoft.EntityFrameworkCore;
using NextGenCoaching.Core.Persistence;

namespace NextGenCoaching.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/health/live", () => Results.Ok(new { status = "live" })).AllowAnonymous();
        routes.MapGet("/health/ready", async (AppDbContext db, CancellationToken ct) =>
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
            deadline.CancelAfter(TimeSpan.FromSeconds(3));
            try
            {
                // Query an application table: connecting alone does not prove migrations ran.
                await db.Courses.AsNoTracking().Select(c => c.Id).Take(1).ToListAsync(deadline.Token);
                return Results.Ok(new { status = "ready", database = "connected" });
            }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
                return Results.Json(new { status = "unavailable", database = "unavailable" }, statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }).AllowAnonymous();
        return routes;
    }
}
