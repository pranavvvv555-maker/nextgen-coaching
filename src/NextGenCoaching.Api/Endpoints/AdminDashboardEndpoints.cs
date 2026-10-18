using Microsoft.EntityFrameworkCore;
using NextGenCoaching.Core.Persistence;
using NextGenCoaching.Shared.Constants;
using NextGenCoaching.Shared.Dtos;
using NextGenCoaching.Shared.Enums;
using NextGenCoaching.Shared.Responses;
using NextGenCoaching.Core.Enums;

namespace NextGenCoaching.Api.Endpoints;

public static class AdminDashboardEndpoints
{
    public static IEndpointRouteBuilder MapAdminDashboardEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(ApiRoutes.Admin.Reports)
            .WithTags("Admin · Dashboard")
            .RequireAuthorization("RequireCounsellor");

        group.MapGet("/dashboard", async (AppDbContext db, CancellationToken ct) =>
        {
            var totalStudents   = await db.StudentProfiles.CountAsync(ct);
            var activeBatches   = await db.Batches.CountAsync(b => b.Status == BatchStatus.Active, ct);
            var totalCourses    = await db.Courses.CountAsync(c => c.IsPublished, ct);
            var placedStudents  = await db.Placements.Select(p => p.StudentProfileId).Distinct().CountAsync(ct);
            var openLeads       = await db.Leads.CountAsync(l =>
                l.Status != LeadStatus.Enrolled &&
                l.Status != LeadStatus.Lost &&
                l.Status != LeadStatus.Dormant, ct);

            var leadsByStatus = await db.Leads
                .GroupBy(l => l.Status)
                .Select(g => new LeadsByStatusItem(g.Key.ToString(), g.Count()))
                .ToListAsync(ct);

            var recentLeads = await db.Leads.AsNoTracking()
                .Include(l => l.CourseInterest)
                .OrderByDescending(l => l.CreatedAtUtc).Take(8)
                .Select(l => new RecentLeadItem(
                    l.Id,
                    l.FullName,
                    l.CourseInterest != null ? l.CourseInterest.Title : "—",
                    l.Status.ToString(),
                    l.CreatedAtUtc))
                .ToListAsync(ct);

            var upcomingMocks = await db.MockInterviews.AsNoTracking()
                .Include(m => m.Enrollment).ThenInclude(e => e.Batch).ThenInclude(b => b.Course)
                .Where(m => m.Status == MockInterviewStatus.Scheduled && m.ScheduledAtUtc > DateTime.UtcNow)
                .OrderBy(m => m.ScheduledAtUtc).Take(8)
                .Select(m => new UpcomingMockInterviewItem(
                    m.Id,
                    "Student",      // resolved properly once we join StudentProfile -> User in a follow-up
                    m.Enrollment.Batch.Course.Title,
                    m.ScheduledAtUtc))
                .ToListAsync(ct);

            return Results.Ok(ApiResponse<AdminDashboardStatsDto>.Ok(new AdminDashboardStatsDto
            {
                TotalStudents = totalStudents,
                ActiveBatches = activeBatches,
                TotalCourses = totalCourses,
                PlacedStudents = placedStudents,
                OpenLeads = openLeads,
                LeadsByStatus = leadsByStatus,
                RecentLeads = recentLeads,
                UpcomingMockInterviews = upcomingMocks
            }));
        }).WithName("AdminGetDashboard");

        return routes;
    }
}
