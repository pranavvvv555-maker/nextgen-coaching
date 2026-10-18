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

public static class AdminMockInterviewsEndpoints
{
    public static IEndpointRouteBuilder MapAdminMockInterviewsEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(ApiRoutes.Admin.MockInterviews)
            .WithTags("Admin/MockInterviews")
            .RequireAuthorization("RequireCounsellor");

        // ---------- Enrollment lookup for the schedule form ----------
        group.MapGet("/enrollments", async (AppDbContext db, CancellationToken ct) =>
        {
            var rows = await db.Enrollments.AsNoTracking()
                .Include(e => e.StudentProfile).ThenInclude(sp => sp.User)
                .Include(e => e.Batch).ThenInclude(b => b.Course)
                .Where(e => e.Status == EnrollmentStatus.Active)
                .OrderBy(e => e.StudentProfile.User!.FullName)
                .Select(e => new SelectOptionDto
                {
                    Id = e.Id,
                    Label = e.StudentProfile.User!.FullName,
                    Sublabel = e.Batch.Course.Title
                })
                .ToListAsync(ct);
            return Results.Ok(ApiResponse<IReadOnlyList<SelectOptionDto>>.Ok(rows));
        }).WithName("AdminMockEnrollmentsLookup");

        // ---------- List ----------
        group.MapGet("/", async (string? status, AppDbContext db, CancellationToken ct) =>
        {
            var rows = await db.MockInterviews.AsNoTracking()
                .Include(m => m.Enrollment).ThenInclude(e => e.StudentProfile).ThenInclude(sp => sp.User)
                .Include(m => m.Enrollment).ThenInclude(e => e.Batch).ThenInclude(b => b.Course)
                .OrderByDescending(m => m.ScheduledAtUtc)
                .ToListAsync(ct);

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<MockInterviewStatus>(status, true, out var s))
                rows = rows.Where(m => m.Status == s).ToList();

            // Resolve interviewer names (no nav property on MockInterview for the interviewer).
            var interviewerIds = rows.Where(m => m.InterviewerId.HasValue).Select(m => m.InterviewerId!.Value).Distinct().ToList();
            var interviewers = await db.InstructorProfiles.AsNoTracking().Include(p => p.User)
                .Where(p => interviewerIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.User != null ? p.User.FullName : string.Empty, ct);

            var dto = rows.Select(m => new AdminMockInterviewListItemDto
            {
                Id = m.Id,
                StudentName = m.Enrollment.StudentProfile.User?.FullName ?? string.Empty,
                CourseTitle = m.Enrollment.Batch.Course.Title,
                RoundNumber = m.RoundNumber,
                ScheduledAtUtc = m.ScheduledAtUtc,
                DurationMinutes = m.DurationMinutes,
                InterviewerName = m.InterviewerId.HasValue && interviewers.TryGetValue(m.InterviewerId.Value, out var n) ? n : null,
                Status = m.Status.ToString(),
                OverallRating = m.OverallRating
            }).ToList();

            return Results.Ok(ApiResponse<IReadOnlyList<AdminMockInterviewListItemDto>>.Ok(dto));
        }).WithName("AdminMockInterviewsList");

        // ---------- Schedule ----------
        group.MapPost("/", async (ScheduleMockInterviewRequest req, AppDbContext db, CancellationToken ct) =>
        {
            var enrollment = await db.Enrollments.AsNoTracking().FirstOrDefaultAsync(e => e.Id == req.EnrollmentId, ct);
            if (enrollment is null) return Results.BadRequest(ApiResponse<Guid>.Fail("Enrollment not found."));

            var m = new MockInterview
            {
                EnrollmentId = req.EnrollmentId,
                InterviewerId = req.InterviewerId,
                RoundNumber = req.RoundNumber,
                ScheduledAtUtc = req.ScheduledAtUtc,
                DurationMinutes = req.DurationMinutes,
                MeetingUrl = req.MeetingUrl,
                Status = MockInterviewStatus.Scheduled
            };
            db.MockInterviews.Add(m);

            // Notify the student
            var studentUserId = await db.StudentProfiles.AsNoTracking()
                .Where(p => p.Id == enrollment.StudentProfileId)
                .Select(p => (Guid?)p.UserId)
                .FirstOrDefaultAsync(ct);
            if (studentUserId is not null)
            {
                db.Notifications.Add(new Notification
                {
                    UserId = studentUserId.Value,
                    Type = "MockInterviewScheduled",
                    Title = $"Mock interview round {req.RoundNumber} scheduled",
                    Message = $"Your mock interview is scheduled for {req.ScheduledAtUtc:dd MMM yyyy, HH:mm} UTC.",
                    LinkUrl = "/student/mock-interviews",
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<Guid>.Ok(m.Id, "Mock interview scheduled."));
        }).WithName("AdminScheduleMockInterview");

        // ---------- Record feedback ----------
        group.MapPut("/{id:guid}/feedback", async (Guid id, RecordMockInterviewFeedbackRequest req, AppDbContext db, CancellationToken ct) =>
        {
            var m = await db.MockInterviews
                .Include(x => x.Enrollment)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (m is null) return Results.NotFound(ApiResponse<object>.Fail("Mock interview not found"));

            if (!Enum.TryParse<MockInterviewStatus>(req.Status, true, out var newStatus))
                return Results.BadRequest(ApiResponse<object>.Fail("Invalid status."));

            m.Status = newStatus;
            m.TechnicalRating = req.TechnicalRating;
            m.CommunicationRating = req.CommunicationRating;
            m.ProblemSolvingRating = req.ProblemSolvingRating;
            m.OverallRating = req.OverallRating;
            m.Strengths = req.Strengths;
            m.AreasToImprove = req.AreasToImprove;
            m.RecordingUrl = req.RecordingUrl;
            m.RecommendedNextSteps = req.RecommendedNextSteps;

            // Notify the student that feedback is available
            if (newStatus == MockInterviewStatus.Completed)
            {
                var studentUserId = await db.StudentProfiles.AsNoTracking()
                    .Where(p => p.Id == m.Enrollment.StudentProfileId)
                    .Select(p => (Guid?)p.UserId)
                    .FirstOrDefaultAsync(ct);
                if (studentUserId is not null)
                {
                    db.Notifications.Add(new Notification
                    {
                        UserId = studentUserId.Value,
                        Type = "MockInterviewFeedback",
                        Title = $"Feedback ready for mock interview round {m.RoundNumber}",
                        Message = "Your interviewer has shared feedback. Sign in to view it.",
                        LinkUrl = "/student/mock-interviews",
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }
            }

            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<object>.Ok(new { m.Id, status = m.Status.ToString() }, "Feedback recorded."));
        }).WithName("AdminRecordMockFeedback");

        return routes;
    }
}
