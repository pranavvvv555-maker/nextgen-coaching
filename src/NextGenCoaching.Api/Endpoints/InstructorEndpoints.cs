using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using NextGenCoaching.Core.Entities;
using NextGenCoaching.Core.Enums;
using NextGenCoaching.Core.Interfaces;
using NextGenCoaching.Core.Persistence;
using NextGenCoaching.Core.Services.Email;
using NextGenCoaching.Shared.Constants;
using NextGenCoaching.Shared.Dtos;
using NextGenCoaching.Shared.Enums;
using NextGenCoaching.Shared.Requests;
using NextGenCoaching.Shared.Responses;

namespace NextGenCoaching.Api.Endpoints;

public static class InstructorEndpoints
{
    public static IEndpointRouteBuilder MapInstructorEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(ApiRoutes.Base + "/instructor")
            .WithTags("Instructor")
            .RequireAuthorization("RequireInstructor");

        // ============================================================
        // DASHBOARD
        // ============================================================

        group.MapGet("/dashboard", async (
            ICurrentUserService currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null) return Results.Unauthorized();

            var profile = await db.InstructorProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId.Value, ct);
            if (profile is null)
                return Results.NotFound(ApiResponse<InstructorDashboardDto>.Fail("No instructor profile for this user"));

            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId.Value, ct);

            // My batches
            var batches = await db.Batches.AsNoTracking()
                .Include(b => b.Course)
                .Include(b => b.Sessions)
                .Include(b => b.Enrollments)
                .Where(b => b.PrimaryInstructorId == profile.Id)
                .ToListAsync(ct);

            var batchIds = batches.Select(b => b.Id).ToList();
            var courseIds = batches.Select(b => b.CourseId).Distinct().ToList();
            var enrollmentIds = batches.SelectMany(b => b.Enrollments.Select(e => e.Id)).ToList();

            var batchSummaries = batches.Select(b => new InstructorBatchSummaryDto
            {
                Id = b.Id,
                Name = b.Name,
                CourseTitle = b.Course.Title,
                Status = b.Status.ToString(),
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                StudentCount = b.Enrollments.Count,
                CompletedSessions = b.Sessions.Count(s => s.Status == SessionStatus.Completed),
                TotalSessions = b.Sessions.Count
            }).ToList();

            // Today's sessions
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);
            var todays = await db.Sessions.AsNoTracking()
                .Include(s => s.Batch).ThenInclude(b => b.Course)
                .Where(s => batchIds.Contains(s.BatchId)
                            && s.ScheduledAtUtc >= today && s.ScheduledAtUtc < tomorrow)
                .OrderBy(s => s.ScheduledAtUtc)
                .Select(s => new InstructorTodaySessionDto
                {
                    Id = s.Id,
                    BatchId = s.BatchId,
                    BatchName = s.Batch.Name,
                    CourseTitle = s.Batch.Course.Title,
                    Title = s.Title,
                    ScheduledAtUtc = s.ScheduledAtUtc,
                    DurationMinutes = s.DurationMinutes,
                    MeetingUrl = s.MeetingUrl
                })
                .ToListAsync(ct);

            // Pending assignment grading — oldest first
            var pendingAssignments = await db.AssignmentSubmissions.AsNoTracking()
                .Include(s => s.Assignment).ThenInclude(a => a.Course)
                .Include(s => s.Enrollment).ThenInclude(e => e.Batch)
                .Include(s => s.Enrollment).ThenInclude(e => e.StudentProfile).ThenInclude(p => p!.User)
                .Where(s => s.Status == SubmissionStatus.Submitted
                            && batchIds.Contains(s.Enrollment.BatchId))
                .OrderBy(s => s.SubmittedAtUtc)
                .Take(5)
                .Select(s => new GradingQueueItemDto
                {
                    SubmissionId = s.Id,
                    Kind = "Assignment",
                    Title = s.Assignment.Title,
                    CourseTitle = s.Assignment.Course.Title,
                    BatchName = s.Enrollment.Batch.Name,
                    StudentName = s.Enrollment.StudentProfile != null && s.Enrollment.StudentProfile.User != null
                                  ? s.Enrollment.StudentProfile.User.FullName : "—",
                    SubmittedAtUtc = s.SubmittedAtUtc,
                    Status = s.Status.ToString(),
                    MaxScore = s.Assignment.MaxScore
                })
                .ToListAsync(ct);

            // Pending project grading
            var pendingProjects = await db.ProjectSubmissions.AsNoTracking()
                .Include(s => s.Project).ThenInclude(p => p.Course)
                .Include(s => s.Enrollment).ThenInclude(e => e.Batch)
                .Include(s => s.Enrollment).ThenInclude(e => e.StudentProfile).ThenInclude(p => p!.User)
                .Where(s => s.Status == SubmissionStatus.Submitted
                            && batchIds.Contains(s.Enrollment.BatchId))
                .OrderBy(s => s.SubmittedAtUtc)
                .Take(5)
                .Select(s => new GradingQueueItemDto
                {
                    SubmissionId = s.Id,
                    Kind = "Project",
                    Title = s.Project.Title,
                    CourseTitle = s.Project.Course.Title,
                    BatchName = s.Enrollment.Batch.Name,
                    StudentName = s.Enrollment.StudentProfile != null && s.Enrollment.StudentProfile.User != null
                                  ? s.Enrollment.StudentProfile.User.FullName : "—",
                    SubmittedAtUtc = s.SubmittedAtUtc,
                    Status = s.Status.ToString(),
                    MaxScore = 100
                })
                .ToListAsync(ct);

            var totalPendingA = await db.AssignmentSubmissions.AsNoTracking()
                .CountAsync(s => s.Status == SubmissionStatus.Submitted && batchIds.Contains(s.Enrollment.BatchId), ct);
            var totalPendingP = await db.ProjectSubmissions.AsNoTracking()
                .CountAsync(s => s.Status == SubmissionStatus.Submitted && batchIds.Contains(s.Enrollment.BatchId), ct);

            var dto = new InstructorDashboardDto
            {
                FullName = user?.FullName ?? "Instructor",
                Stats = new InstructorStatsDto
                {
                    ActiveBatches = batches.Count(b => b.Status == BatchStatus.Active),
                    TotalStudents = enrollmentIds.Count,
                    PendingAssignmentGrades = totalPendingA,
                    PendingProjectGrades = totalPendingP
                },
                Batches = batchSummaries,
                TodaysSessions = todays,
                PendingAssignments = pendingAssignments,
                PendingProjects = pendingProjects
            };

            return Results.Ok(ApiResponse<InstructorDashboardDto>.Ok(dto));
        }).WithName("InstructorDashboard");

        // ============================================================
        // BATCHES
        // ============================================================

        group.MapGet("/batches", async (
            ICurrentUserService currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null) return Results.Unauthorized();
            var profile = await db.InstructorProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId.Value, ct);
            if (profile is null)
                return Results.Ok(ApiResponse<IReadOnlyList<InstructorBatchSummaryDto>>.Ok(Array.Empty<InstructorBatchSummaryDto>()));

            var batches = await db.Batches.AsNoTracking()
                .Include(b => b.Course)
                .Include(b => b.Sessions)
                .Include(b => b.Enrollments)
                .Where(b => b.PrimaryInstructorId == profile.Id)
                .OrderByDescending(b => b.StartDate)
                .Select(b => new InstructorBatchSummaryDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    CourseTitle = b.Course.Title,
                    Status = b.Status.ToString(),
                    StartDate = b.StartDate,
                    EndDate = b.EndDate,
                    StudentCount = b.Enrollments.Count,
                    CompletedSessions = b.Sessions.Count(s => s.Status == SessionStatus.Completed),
                    TotalSessions = b.Sessions.Count
                })
                .ToListAsync(ct);

            return Results.Ok(ApiResponse<IReadOnlyList<InstructorBatchSummaryDto>>.Ok(batches));
        }).WithName("InstructorListBatches");

        // ============================================================
        // ASSIGNMENT GRADING
        // ============================================================

        group.MapGet("/assignments/pending", async (
            ICurrentUserService currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null) return Results.Unauthorized();
            var profile = await db.InstructorProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId.Value, ct);
            if (profile is null)
                return Results.Ok(ApiResponse<IReadOnlyList<GradingQueueItemDto>>.Ok(Array.Empty<GradingQueueItemDto>()));

            var batchIds = await db.Batches.AsNoTracking()
                .Where(b => b.PrimaryInstructorId == profile.Id)
                .Select(b => b.Id).ToListAsync(ct);

            var now = DateTime.UtcNow;
            var rows = await db.AssignmentSubmissions.AsNoTracking()
                .Include(s => s.Assignment).ThenInclude(a => a.Course)
                .Include(s => s.Enrollment).ThenInclude(e => e.Batch)
                .Include(s => s.Enrollment).ThenInclude(e => e.StudentProfile).ThenInclude(p => p!.User)
                .Where(s => s.Status == SubmissionStatus.Submitted
                            && batchIds.Contains(s.Enrollment.BatchId))
                .OrderBy(s => s.SubmittedAtUtc)
                .Select(s => new GradingQueueItemDto
                {
                    SubmissionId = s.Id,
                    Kind = "Assignment",
                    Title = s.Assignment.Title,
                    CourseTitle = s.Assignment.Course.Title,
                    BatchName = s.Enrollment.Batch.Name,
                    StudentName = s.Enrollment.StudentProfile != null && s.Enrollment.StudentProfile.User != null
                                  ? s.Enrollment.StudentProfile.User.FullName : "—",
                    SubmittedAtUtc = s.SubmittedAtUtc,
                    AgeHours = (int)(now - s.SubmittedAtUtc).TotalHours,
                    Status = s.Status.ToString(),
                    MaxScore = s.Assignment.MaxScore
                })
                .ToListAsync(ct);

            return Results.Ok(ApiResponse<IReadOnlyList<GradingQueueItemDto>>.Ok(rows));
        }).WithName("InstructorPendingAssignments");

        group.MapGet("/submissions/{id:guid}", async (
            Guid id,
            ICurrentUserService currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (!await EnsureInstructorOwnsAssignmentSubmissionAsync(id, currentUser, db, ct))
                return Results.NotFound(ApiResponse<AssignmentGradingDto>.Fail("Submission not found in your batches"));

            var sub = await db.AssignmentSubmissions.AsNoTracking()
                .Include(s => s.Assignment).ThenInclude(a => a.Course)
                .Include(s => s.Enrollment).ThenInclude(e => e.Batch)
                .Include(s => s.Enrollment).ThenInclude(e => e.StudentProfile).ThenInclude(p => p!.User)
                .FirstAsync(s => s.Id == id, ct);

            var dto = new AssignmentGradingDto
            {
                SubmissionId = sub.Id,
                AssignmentId = sub.AssignmentId,
                AssignmentNumber = sub.Assignment.AssignmentNumber,
                AssignmentTitle = sub.Assignment.Title,
                AssignmentBrief = sub.Assignment.Description,
                MaxScore = sub.Assignment.MaxScore,
                CourseTitle = sub.Assignment.Course.Title,
                BatchName = sub.Enrollment.Batch.Name,
                StudentName = sub.Enrollment.StudentProfile?.User?.FullName ?? "—",
                StudentEmail = sub.Enrollment.StudentProfile?.User?.Email ?? string.Empty,
                SubmissionUrl = sub.SubmissionUrl,
                SubmissionText = sub.SubmissionText,
                SubmittedAtUtc = sub.SubmittedAtUtc,
                Status = sub.Status.ToString(),
                Score = sub.Score,
                Feedback = sub.Feedback,
                GradedAtUtc = sub.GradedAtUtc
            };
            return Results.Ok(ApiResponse<AssignmentGradingDto>.Ok(dto));
        }).WithName("InstructorGetAssignmentSubmission");

        group.MapPost("/submissions/{id:guid}/grade", async (
            Guid id,
            GradeAssignmentRequest req,
            ICurrentUserService currentUser,
            AppDbContext db,
            IEmailDispatcher dispatcher,
            CancellationToken ct) =>
        {
            if (!await EnsureInstructorOwnsAssignmentSubmissionAsync(id, currentUser, db, ct))
                return Results.NotFound(ApiResponse<object>.Fail("Submission not found in your batches"));

            var sub = await db.AssignmentSubmissions
                .Include(s => s.Assignment)
                .Include(s => s.Enrollment).ThenInclude(e => e.StudentProfile).ThenInclude(sp => sp!.User)
                .FirstAsync(s => s.Id == id, ct);

            if (req.Score > sub.Assignment.MaxScore)
                return Results.BadRequest(ApiResponse<object>.Fail($"Score can't exceed max ({sub.Assignment.MaxScore})"));

            sub.Score = req.Score;
            sub.Feedback = req.Feedback;
            sub.Status = SubmissionStatus.Graded;
            sub.GradedAtUtc = DateTime.UtcNow;
            sub.GradedByUserId = currentUser.UserId;

            // Notify the student
            var studentUserId = sub.Enrollment.StudentProfile?.UserId;
            if (studentUserId is not null)
            {
                db.Notifications.Add(new Notification
                {
                    UserId = studentUserId.Value,
                    Type = "AssignmentGraded",
                    Title = $"Assignment graded: {sub.Assignment.Title}",
                    Message = $"You scored {req.Score:0.#} / {sub.Assignment.MaxScore}.",
                    LinkUrl = $"/student/assignments/{sub.AssignmentId}",
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync(ct);

            var aEmail = sub.Enrollment.StudentProfile?.User?.Email;
            if (!string.IsNullOrEmpty(aEmail))
            {
                await dispatcher.SendAsync("assignment_graded", aEmail, new Dictionary<string, string?>
                {
                    ["full_name"] = sub.Enrollment.StudentProfile?.User?.FullName,
                    ["item_title"] = sub.Assignment.Title,
                    ["score"] = req.Score.ToString("0.#"),
                    ["max_score"] = sub.Assignment.MaxScore.ToString("0.#")
                }, ct);
            }

            return Results.Ok(ApiResponse<object>.Ok(new { sub.Id, status = sub.Status.ToString() }, "Graded"));
        }).WithName("InstructorGradeAssignment");

        // ============================================================
        // PROJECT GRADING
        // ============================================================

        group.MapGet("/projects/pending", async (
            ICurrentUserService currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null) return Results.Unauthorized();
            var profile = await db.InstructorProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId.Value, ct);
            if (profile is null)
                return Results.Ok(ApiResponse<IReadOnlyList<GradingQueueItemDto>>.Ok(Array.Empty<GradingQueueItemDto>()));

            var batchIds = await db.Batches.AsNoTracking()
                .Where(b => b.PrimaryInstructorId == profile.Id)
                .Select(b => b.Id).ToListAsync(ct);

            var now = DateTime.UtcNow;
            var rows = await db.ProjectSubmissions.AsNoTracking()
                .Include(s => s.Project).ThenInclude(p => p.Course)
                .Include(s => s.Enrollment).ThenInclude(e => e.Batch)
                .Include(s => s.Enrollment).ThenInclude(e => e.StudentProfile).ThenInclude(p => p!.User)
                .Where(s => s.Status == SubmissionStatus.Submitted
                            && batchIds.Contains(s.Enrollment.BatchId))
                .OrderBy(s => s.SubmittedAtUtc)
                .Select(s => new GradingQueueItemDto
                {
                    SubmissionId = s.Id,
                    Kind = "Project",
                    Title = s.Project.Title,
                    CourseTitle = s.Project.Course.Title,
                    BatchName = s.Enrollment.Batch.Name,
                    StudentName = s.Enrollment.StudentProfile != null && s.Enrollment.StudentProfile.User != null
                                  ? s.Enrollment.StudentProfile.User.FullName : "—",
                    SubmittedAtUtc = s.SubmittedAtUtc,
                    AgeHours = (int)(now - s.SubmittedAtUtc).TotalHours,
                    Status = s.Status.ToString(),
                    MaxScore = 100
                })
                .ToListAsync(ct);

            return Results.Ok(ApiResponse<IReadOnlyList<GradingQueueItemDto>>.Ok(rows));
        }).WithName("InstructorPendingProjects");

        group.MapGet("/project-submissions/{id:guid}", async (
            Guid id,
            ICurrentUserService currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (!await EnsureInstructorOwnsProjectSubmissionAsync(id, currentUser, db, ct))
                return Results.NotFound(ApiResponse<ProjectGradingDto>.Fail("Submission not found in your batches"));

            var sub = await db.ProjectSubmissions.AsNoTracking()
                .Include(s => s.Project).ThenInclude(p => p.Course)
                .Include(s => s.Enrollment).ThenInclude(e => e.Batch)
                .Include(s => s.Enrollment).ThenInclude(e => e.StudentProfile).ThenInclude(p => p!.User)
                .FirstAsync(s => s.Id == id, ct);

            var dto = new ProjectGradingDto
            {
                SubmissionId = sub.Id,
                ProjectId = sub.ProjectId,
                ProjectNumber = sub.Project.ProjectNumber,
                ProjectTitle = sub.Project.Title,
                ProjectBrief = sub.Project.Brief,
                RubricJson = sub.Project.RubricJson,
                CourseTitle = sub.Project.Course.Title,
                BatchName = sub.Enrollment.Batch.Name,
                StudentName = sub.Enrollment.StudentProfile?.User?.FullName ?? "—",
                StudentEmail = sub.Enrollment.StudentProfile?.User?.Email ?? string.Empty,
                RepoUrl = sub.RepoUrl,
                DeployedUrl = sub.DeployedUrl,
                PresentationUrl = sub.PresentationUrl,
                Description = sub.Description,
                SubmittedAtUtc = sub.SubmittedAtUtc,
                Status = sub.Status.ToString(),
                TotalScore = sub.TotalScore,
                Feedback = sub.Feedback,
                EvaluationJson = sub.EvaluationJson,
                GradedAtUtc = sub.GradedAtUtc
            };
            return Results.Ok(ApiResponse<ProjectGradingDto>.Ok(dto));
        }).WithName("InstructorGetProjectSubmission");

        group.MapPost("/project-submissions/{id:guid}/grade", async (
            Guid id,
            GradeProjectRequest req,
            ICurrentUserService currentUser,
            AppDbContext db,
            IEmailDispatcher dispatcher,
            CancellationToken ct) =>
        {
            if (!await EnsureInstructorOwnsProjectSubmissionAsync(id, currentUser, db, ct))
                return Results.NotFound(ApiResponse<object>.Fail("Submission not found in your batches"));

            var sub = await db.ProjectSubmissions
                .Include(s => s.Project)
                .Include(s => s.Enrollment).ThenInclude(e => e.StudentProfile).ThenInclude(sp => sp!.User)
                .FirstAsync(s => s.Id == id, ct);

            sub.TotalScore = req.TotalScore;
            sub.Feedback = req.Feedback;
            sub.EvaluationJson = req.EvaluationJson;
            sub.Status = SubmissionStatus.Graded;
            sub.GradedAtUtc = DateTime.UtcNow;
            sub.GradedByUserId = currentUser.UserId;

            var studentUserId = sub.Enrollment.StudentProfile?.UserId;
            if (studentUserId is not null)
            {
                db.Notifications.Add(new Notification
                {
                    UserId = studentUserId.Value,
                    Type = "ProjectGraded",
                    Title = $"Project graded: {sub.Project.Title}",
                    Message = $"You scored {req.TotalScore:0.#}.",
                    LinkUrl = $"/student/projects/{sub.ProjectId}",
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync(ct);

            var pEmail = sub.Enrollment.StudentProfile?.User?.Email;
            if (!string.IsNullOrEmpty(pEmail))
            {
                await dispatcher.SendAsync("project_graded", pEmail, new Dictionary<string, string?>
                {
                    ["full_name"] = sub.Enrollment.StudentProfile?.User?.FullName,
                    ["item_title"] = sub.Project.Title,
                    ["score"] = req.TotalScore.ToString("0.#")
                }, ct);
            }

            return Results.Ok(ApiResponse<object>.Ok(new { sub.Id, status = sub.Status.ToString() }, "Graded"));
        }).WithName("InstructorGradeProject");

        // ============================================================
        // MY PROFILE
        // ============================================================

        group.MapGet("/profile", async (ICurrentUserService currentUser, AppDbContext db, CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null) return Results.Unauthorized();

            var p = await db.InstructorProfiles.AsNoTracking()
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.UserId == userId.Value, ct);
            if (p is null) return Results.NotFound(ApiResponse<InstructorProfileDto>.Fail("No instructor profile"));

            var dto = new InstructorProfileDto
            {
                FullName = p.User?.FullName ?? string.Empty,
                Email = p.User?.Email ?? string.Empty,
                Phone = p.User?.PhoneNumber,
                City = p.User?.City,
                Headline = p.Headline,
                Bio = p.Bio,
                ExpertiseTags = p.ExpertiseTags,
                YearsExperience = p.YearsExperience,
                LinkedinUrl = p.LinkedinUrl,
                HourlyRate = p.HourlyRate,
                IsPublic = p.IsPublic
            };
            return Results.Ok(ApiResponse<InstructorProfileDto>.Ok(dto));
        }).WithName("InstructorGetProfile");

        group.MapPut("/profile", async (UpdateInstructorRequest req, ICurrentUserService currentUser, AppDbContext db, CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null) return Results.Unauthorized();

            var p = await db.InstructorProfiles.Include(x => x.User).FirstOrDefaultAsync(x => x.UserId == userId.Value, ct);
            if (p is null) return Results.NotFound(ApiResponse<object>.Fail("No instructor profile"));

            p.Headline = req.Headline;
            p.Bio = req.Bio;
            p.ExpertiseTags = req.ExpertiseTags ?? new();
            p.YearsExperience = req.YearsExperience;
            p.LinkedinUrl = req.LinkedinUrl;
            p.HourlyRate = req.HourlyRate;
            p.IsPublic = req.IsPublic;
            if (p.User is not null)
            {
                p.User.FullName = string.IsNullOrWhiteSpace(req.FullName) ? p.User.FullName : req.FullName.Trim();
                p.User.PhoneNumber = req.Phone?.Trim();
                p.User.City = req.City;
            }
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<object>.Ok(new { p.Id }, "Profile updated."));
        }).WithName("InstructorUpdateProfile");

        // ============================================================
        // ATTENDANCE
        // ============================================================

        group.MapGet("/attendance/sessions", async (ICurrentUserService currentUser, AppDbContext db, CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null) return Results.Unauthorized();
            var profile = await db.InstructorProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId.Value, ct);
            if (profile is null) return Results.Ok(ApiResponse<IReadOnlyList<InstructorSessionListItemDto>>.Ok(new List<InstructorSessionListItemDto>()));

            var batchIds = await db.Batches.AsNoTracking()
                .Where(b => b.PrimaryInstructorId == profile.Id)
                .Select(b => b.Id).ToListAsync(ct);

            var sessions = await db.Sessions.AsNoTracking()
                .Include(s => s.Batch).ThenInclude(b => b.Course)
                .Where(s => batchIds.Contains(s.BatchId))
                .OrderByDescending(s => s.ScheduledAtUtc)
                .ToListAsync(ct);

            var enrollCounts = await db.Enrollments.AsNoTracking()
                .Where(e => batchIds.Contains(e.BatchId) && e.Status == EnrollmentStatus.Active)
                .GroupBy(e => e.BatchId)
                .Select(g => new { BatchId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.BatchId, x => x.Count, ct);

            var sessionIds = sessions.Select(s => s.Id).ToList();
            var markedCounts = await db.SessionAttendances.AsNoTracking()
                .Where(a => sessionIds.Contains(a.SessionId))
                .GroupBy(a => a.SessionId)
                .Select(g => new { SessionId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SessionId, x => x.Count, ct);

            var dto = sessions.Select(s => new InstructorSessionListItemDto
            {
                Id = s.Id,
                Title = s.Title,
                BatchName = s.Batch.Name,
                CourseTitle = s.Batch.Course.Title,
                ScheduledAtUtc = s.ScheduledAtUtc,
                RosterSize = enrollCounts.TryGetValue(s.BatchId, out var rc) ? rc : 0,
                MarkedCount = markedCounts.TryGetValue(s.Id, out var mc) ? mc : 0
            }).ToList();

            return Results.Ok(ApiResponse<IReadOnlyList<InstructorSessionListItemDto>>.Ok(dto));
        }).WithName("InstructorAttendanceSessions");

        group.MapGet("/attendance/sessions/{sessionId:guid}", async (Guid sessionId, ICurrentUserService currentUser, AppDbContext db, CancellationToken ct) =>
        {
            var session = await db.Sessions.AsNoTracking().Include(s => s.Batch).FirstOrDefaultAsync(s => s.Id == sessionId, ct);
            if (session is null) return Results.NotFound(ApiResponse<AttendanceRosterDto>.Fail("Session not found"));

            var enrollments = await db.Enrollments.AsNoTracking()
                .Include(e => e.StudentProfile).ThenInclude(sp => sp.User)
                .Where(e => e.BatchId == session.BatchId && e.Status == EnrollmentStatus.Active)
                .ToListAsync(ct);

            var existing = await db.SessionAttendances.AsNoTracking()
                .Where(a => a.SessionId == sessionId)
                .ToDictionaryAsync(a => a.EnrollmentId, a => a.Status, ct);

            var dto = new AttendanceRosterDto
            {
                SessionId = sessionId,
                SessionTitle = session.Title,
                BatchName = session.Batch.Name,
                ScheduledAtUtc = session.ScheduledAtUtc,
                Rows = enrollments.Select(e => new AttendanceRosterRowDto
                {
                    EnrollmentId = e.Id,
                    StudentName = e.StudentProfile.User?.FullName ?? string.Empty,
                    Status = existing.TryGetValue(e.Id, out var st) ? st.ToString() : "Present"
                }).OrderBy(r => r.StudentName).ToList()
            };
            return Results.Ok(ApiResponse<AttendanceRosterDto>.Ok(dto));
        }).WithName("InstructorAttendanceRoster");

        group.MapPost("/attendance/sessions/{sessionId:guid}", async (Guid sessionId, SaveAttendanceRequest req, AppDbContext db, CancellationToken ct) =>
        {
            var session = await db.Sessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == sessionId, ct);
            if (session is null) return Results.NotFound(ApiResponse<object>.Fail("Session not found"));

            var existing = await db.SessionAttendances
                .Where(a => a.SessionId == sessionId)
                .ToListAsync(ct);

            foreach (var entry in req.Entries)
            {
                if (!Enum.TryParse<AttendanceStatus>(entry.Status, true, out var status)) status = AttendanceStatus.Present;
                var row = existing.FirstOrDefault(a => a.EnrollmentId == entry.EnrollmentId);
                if (row is null)
                {
                    db.SessionAttendances.Add(new SessionAttendance
                    {
                        SessionId = sessionId,
                        EnrollmentId = entry.EnrollmentId,
                        Status = status
                    });
                }
                else
                {
                    row.Status = status;
                }
            }
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<object>.Ok(new { sessionId, count = req.Entries.Count }, "Attendance saved."));
        }).WithName("InstructorSaveAttendance");

        return routes;
    }

    // ---------- ownership guards ----------

    private static async Task<bool> EnsureInstructorOwnsAssignmentSubmissionAsync(
        Guid submissionId, ICurrentUserService currentUser, AppDbContext db, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId is null) return false;
        var profile = await db.InstructorProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId.Value, ct);
        if (profile is null) return false;

        return await db.AssignmentSubmissions.AsNoTracking()
            .AnyAsync(s => s.Id == submissionId
                           && s.Enrollment.Batch.PrimaryInstructorId == profile.Id, ct);
    }

    private static async Task<bool> EnsureInstructorOwnsProjectSubmissionAsync(
        Guid submissionId, ICurrentUserService currentUser, AppDbContext db, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId is null) return false;
        var profile = await db.InstructorProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId.Value, ct);
        if (profile is null) return false;

        return await db.ProjectSubmissions.AsNoTracking()
            .AnyAsync(s => s.Id == submissionId
                           && s.Enrollment.Batch.PrimaryInstructorId == profile.Id, ct);
    }
}
