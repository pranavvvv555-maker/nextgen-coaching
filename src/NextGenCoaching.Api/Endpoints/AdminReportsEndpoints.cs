using Microsoft.EntityFrameworkCore;
using NextGenCoaching.Core.Enums;
using NextGenCoaching.Core.Persistence;
using NextGenCoaching.Shared.Constants;
using NextGenCoaching.Shared.Dtos;
using NextGenCoaching.Shared.Enums;
using NextGenCoaching.Shared.Responses;

namespace NextGenCoaching.Api.Endpoints;

public static class AdminReportsEndpoints
{
    public static IEndpointRouteBuilder MapAdminReportsEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(ApiRoutes.Admin.Reports)
            .WithTags("Admin · Reports")
            .RequireAuthorization("RequireCounsellor");

        group.MapGet("/", async (AppDbContext db, CancellationToken ct) =>
        {
            // Pull enrollments once and aggregate in memory (report-scale data).
            var enr = await db.Enrollments.AsNoTracking()
                .Select(e => new { Course = e.Batch.Course.Title, e.AmountPaid, e.EnrolledAtUtc, e.Status })
                .ToListAsync(ct);

            var totalStudents = await db.StudentProfiles.CountAsync(ct);
            var placementsJoined = await db.JobReferrals.CountAsync(r => r.Status == JobReferralStatus.Joined, ct);
            var openJobs = await db.JobOpenings.CountAsync(j => j.Status == JobOpeningStatus.Open, ct);
            var referralStatuses = await db.JobReferrals.AsNoTracking().Select(r => r.Status).ToListAsync(ct);

            var revByCourse = enr
                .GroupBy(x => x.Course)
                .Select(g => new RevenueByCourseRow { CourseTitle = g.Key, Enrollments = g.Count(), Revenue = g.Sum(y => y.AmountPaid) })
                .OrderByDescending(r => r.Revenue)
                .ToList();

            var byMonth = enr
                .GroupBy(x => new { x.EnrolledAtUtc.Year, x.EnrolledAtUtc.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new EnrollmentsByMonthRow { Month = $"{g.Key.Year:0000}-{g.Key.Month:00}", Count = g.Count() })
                .ToList();

            var byStatus = referralStatuses
                .GroupBy(s => s)
                .Select(g => new PlacementsByStatusRow { Status = g.Key.ToString(), Count = g.Count() })
                .OrderByDescending(r => r.Count)
                .ToList();

            var dto = new AdminReportDto
            {
                TotalStudents = totalStudents,
                ActiveEnrollments = enr.Count(x => x.Status == EnrollmentStatus.Active),
                TotalRevenue = enr.Sum(x => x.AmountPaid),
                Placements = placementsJoined,
                OpenJobs = openJobs,
                RevenueByCourse = revByCourse,
                EnrollmentsByMonth = byMonth,
                PlacementsByStatus = byStatus
            };
            return Results.Ok(ApiResponse<AdminReportDto>.Ok(dto));
        }).WithName("AdminReports");

        return routes;
    }
}
