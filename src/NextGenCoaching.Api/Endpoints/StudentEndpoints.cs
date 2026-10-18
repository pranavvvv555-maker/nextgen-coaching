using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using NextGenCoaching.Core.Entities;
using NextGenCoaching.Core.Enums;
using NextGenCoaching.Core.Interfaces;
using NextGenCoaching.Core.Payments;
using NextGenCoaching.Core.Persistence;
using NextGenCoaching.Shared.Constants;
using NextGenCoaching.Shared.Dtos;
using NextGenCoaching.Shared.Requests;
using NextGenCoaching.Shared.Responses;
using SharedEnums = NextGenCoaching.Shared.Enums;

namespace NextGenCoaching.Api.Endpoints;

public static class StudentEndpoints
{
    public static IEndpointRouteBuilder MapStudentEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(ApiRoutes.Base + "/student")
            .WithTags("Student")
            .RequireAuthorization("RequireStudent");

        // GET /api/v1/student/dashboard
        group.MapGet("/dashboard", async (
            ICurrentUserService currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null) return Results.Unauthorized();

            // Resolve the student profile
            var profile = await db.StudentProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId.Value, ct);

            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId.Value, ct);

            if (profile is null || user is null)
                return Results.NotFound(ApiResponse<object>.Fail("Student profile not found"));

            // Load enrollments with course + batch
            var enrollmentRows = await db.Enrollments.AsNoTracking()
                .Include(e => e.Batch).ThenInclude(b => b.Course)
                .Include(e => e.Batch).ThenInclude(b => b.Sessions)
                .Where(e => e.StudentProfileId == profile.Id
                            && e.Status != SharedEnums.EnrollmentStatus.Dropped
                            && e.Status != SharedEnums.EnrollmentStatus.Refunded)
                .ToListAsync(ct);

            var enrollmentDtos = enrollmentRows.Select(e => new StudentEnrollmentDto
            {
                EnrollmentId = e.Id,
                CourseId = e.Batch.CourseId,
                CourseSlug = e.Batch.Course.Slug,
                CourseTitle = e.Batch.Course.Title,
                BatchName = e.Batch.Name,
                BatchStartDate = e.Batch.StartDate,
                BatchEndDate = e.Batch.EndDate,
                CompletionPercentage = e.CompletionPercentage,
                Status = e.Status.ToString(),
                CompletedSessions = e.Batch.Sessions.Count(s => s.Status == SessionStatus.Completed),
                TotalSessions = e.Batch.Sessions.Count
            }).ToList();

            var enrollmentIds = enrollmentRows.Select(e => e.Id).ToList();
            var batchIds = enrollmentRows.Select(e => e.BatchId).ToList();

            // Upcoming sessions across all enrollments — next 8
            var now = DateTime.UtcNow;
            var sessions = await db.Sessions.AsNoTracking()
                .Include(s => s.Batch).ThenInclude(b => b.Course)
                .Where(s => batchIds.Contains(s.BatchId)
                            && s.Status == SessionStatus.Scheduled
                            && s.ScheduledAtUtc > now)
                .OrderBy(s => s.ScheduledAtUtc).Take(8)
                .Select(s => new StudentSessionDto
                {
                    SessionId = s.Id,
                    CourseId = s.Batch.CourseId,
                    CourseSlug = s.Batch.Course.Slug,
                    CourseTitle = s.Batch.Course.Title,
                    SessionType = s.SessionType.ToString(),
                    Title = s.Title,
                    ScheduledAtUtc = s.ScheduledAtUtc,
                    DurationMinutes = s.DurationMinutes,
                    MeetingUrl = s.MeetingUrl,
                    RecordingUrl = s.RecordingUrl,
                    Status = s.Status.ToString()
                })
                .ToListAsync(ct);

            // Pending + recent assignments
            var courseIds = enrollmentRows.Select(e => e.Batch.CourseId).Distinct().ToList();
            var allAssignments = await db.Assignments.AsNoTracking()
                .Include(a => a.Course)
                .Include(a => a.CourseTool)
                .Where(a => courseIds.Contains(a.CourseId) && a.IsPublished)
                .OrderBy(a => a.AssignmentNumber).Take(20)
                .ToListAsync(ct);

            var mySubmissions = await db.AssignmentSubmissions.AsNoTracking()
                .Where(s => enrollmentIds.Contains(s.EnrollmentId))
                .ToListAsync(ct);

            var assignmentDtos = allAssignments.Select(a =>
            {
                var sub = mySubmissions.FirstOrDefault(s => s.AssignmentId == a.Id);
                var enrollment = enrollmentRows.First(e => e.Batch.CourseId == a.CourseId);
                var due = enrollment.EnrolledAtUtc.AddDays(a.DueOffsetDays);
                return new StudentAssignmentDto
                {
                    AssignmentId = a.Id,
                    EnrollmentId = enrollment.Id,
                    SubmissionId = sub?.Id,
                    CourseTitle = a.Course.Title,
                    CourseSlug = a.Course.Slug,
                    Title = a.Title,
                    ToolName = a.CourseTool?.ToolName,
                    MaxScore = a.MaxScore,
                    DueAtUtc = due,
                    Status = sub is null ? "NotStarted" : sub.Status.ToString(),
                    Score = sub?.Score
                };
            }).ToList();

            var pendingAssignments = assignmentDtos
                .Where(a => a.Status == "NotStarted" || a.Status == "Submitted")
                .OrderBy(a => a.DueAtUtc).Take(6)
                .ToList();

            // Mock interviews
            var mocks = await db.MockInterviews.AsNoTracking()
                .Include(m => m.Enrollment).ThenInclude(e => e.Batch).ThenInclude(b => b.Course)
                .Where(m => enrollmentIds.Contains(m.EnrollmentId)
                            && m.Status == MockInterviewStatus.Scheduled
                            && m.ScheduledAtUtc > now)
                .OrderBy(m => m.ScheduledAtUtc).Take(5)
                .Select(m => new StudentMockInterviewDto
                {
                    Id = m.Id,
                    CourseTitle = m.Enrollment.Batch.Course.Title,
                    RoundNumber = m.RoundNumber,
                    ScheduledAtUtc = m.ScheduledAtUtc,
                    DurationMinutes = m.DurationMinutes,
                    MeetingUrl = m.MeetingUrl
                })
                .ToListAsync(ct);

            // Announcements (notifications targeted at this user, type = Announcement)
            var announcements = await db.Notifications.AsNoTracking()
                .Where(n => n.UserId == userId.Value && n.Type == "Announcement")
                .OrderByDescending(n => n.CreatedAtUtc).Take(5)
                .Select(n => new StudentAnnouncementDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message ?? string.Empty,
                    CreatedAtUtc = n.CreatedAtUtc,
                    LinkUrl = n.LinkUrl
                })
                .ToListAsync(ct);

            // Attendance percentage
            var totalAttendable = await db.SessionAttendances.AsNoTracking()
                .CountAsync(a => enrollmentIds.Contains(a.EnrollmentId), ct);
            var present = await db.SessionAttendances.AsNoTracking()
                .CountAsync(a => enrollmentIds.Contains(a.EnrollmentId)
                                  && a.Status == AttendanceStatus.Present, ct);
            var attendancePct = totalAttendable == 0 ? 100m : Math.Round(present * 100m / totalAttendable, 1);

            var dto = new StudentDashboardDto
            {
                Profile = new StudentProfileSummary
                {
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    CurrentRole = profile.CurrentRole,
                    TargetRole = profile.TargetRole,
                    ProfileImageUrl = user.ProfileImageUrl
                },
                Stats = new StudentStatsDto
                {
                    CoursesEnrolled = enrollmentDtos.Count,
                    AssignmentsPending = assignmentDtos.Count(a => a.Status == "NotStarted"),
                    MockInterviewsUpcoming = mocks.Count,
                    AttendancePercentage = attendancePct
                },
                Enrollments = enrollmentDtos,
                UpcomingSessions = sessions,
                PendingAssignments = pendingAssignments,
                UpcomingMockInterviews = mocks,
                Announcements = announcements
            };

            return Results.Ok(ApiResponse<StudentDashboardDto>.Ok(dto));
        }).WithName("StudentDashboard");

        // GET /api/v1/student/payments
        group.MapGet("/payments", async (
            ICurrentUserService currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null) return Results.Unauthorized();

            var profile = await db.StudentProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId.Value, ct);
            if (profile is null)
                return Results.Ok(ApiResponse<IReadOnlyList<PaymentSummaryDto>>.Ok(Array.Empty<PaymentSummaryDto>()));

            var rows = await db.PaymentTransactions.AsNoTracking()
                .Include(t => t.Course)
                .Include(t => t.Batch)
                .Where(t => t.StudentProfileId == profile.Id)
                .OrderByDescending(t => t.CreatedAtUtc)
                .Select(t => new PaymentSummaryDto
                {
                    Id = t.Id,
                    Provider = t.Provider,
                    ProviderOrderId = t.ProviderOrderId,
                    ProviderPaymentId = t.ProviderPaymentId,
                    Amount = t.Amount,
                    Currency = t.Currency,
                    Status = t.Status.ToString(),
                    CourseTitle = t.Course != null ? t.Course.Title : null,
                    BatchName = t.Batch != null ? t.Batch.Name : null,
                    CreatedAtUtc = t.CreatedAtUtc,
                    InvoiceAvailable = t.Status == PaymentStatus.Captured || t.Status == PaymentStatus.Refunded
                })
                .ToListAsync(ct);

            return Results.Ok(ApiResponse<IReadOnlyList<PaymentSummaryDto>>.Ok(rows));
        }).WithName("StudentListPayments");

        // GET /api/v1/student/payments/{id}/invoice — only their own
        group.MapGet("/payments/{id:guid}/invoice", async (
            Guid id,
            ICurrentUserService currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null) return Results.Unauthorized();

            var profile = await db.StudentProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId.Value, ct);
            if (profile is null) return Results.NotFound();

            var tx = await db.PaymentTransactions.AsNoTracking()
                .Include(t => t.Course)
                .Include(t => t.Batch)
                .Include(t => t.StudentProfile).ThenInclude(p => p!.User)
                .FirstOrDefaultAsync(t => t.Id == id && t.StudentProfileId == profile.Id, ct);
            if (tx is null) return Results.NotFound();

            if (tx.Status != PaymentStatus.Captured && tx.Status != PaymentStatus.Refunded)
                return Results.BadRequest(ApiResponse<object>.Fail("Invoice only available for captured payments"));

            var data = new InvoiceData
            {
                InvoiceNumber = $"NG-{tx.CreatedAtUtc:yyMM}-{tx.Id.ToString()[..8].ToUpperInvariant()}",
                IssuedAt = tx.CreatedAtUtc,
                CustomerName = tx.StudentProfile?.User?.FullName ?? "Customer",
                CustomerEmail = tx.StudentProfile?.User?.Email ?? string.Empty,
                CustomerPhone = tx.StudentProfile?.User?.PhoneNumber,
                CourseTitle = tx.Course?.Title ?? "NextGen course",
                BatchName = tx.Batch?.Name,
                Amount = tx.Amount,
                Currency = tx.Currency,
                Status = tx.Status.ToString(),
                ProviderOrderId = tx.ProviderOrderId,
                ProviderPaymentId = tx.ProviderPaymentId
            };
            var bytes = InvoiceGenerator.Generate(data);
            return Results.File(bytes, "application/pdf", $"{data.InvoiceNumber}.pdf");
        }).WithName("StudentDownloadInvoice");

        // ============================================================
        // ASSIGNMENT DETAIL + SUBMISSION
        // ============================================================

        group.MapGet("/assignments/{id:guid}", async (
            Guid id,
            ICurrentUserService currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null) return Results.Unauthorized();

            var profile = await db.StudentProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId.Value, ct);
            if (profile is null) return Results.NotFound(ApiResponse<AssignmentDetailDto>.Fail("No student profile"));

            var assignment = await db.Assignments.AsNoTracking()
                .Include(a => a.Course)
                .Include(a => a.CourseTool)
                .FirstOrDefaultAsync(a => a.Id == id && a.IsPublished, ct);
            if (assignment is null) return Results.NotFound(ApiResponse<AssignmentDetailDto>.Fail("Assignment not found"));

            // Find the student's enrollment for that course (if any) and their submission
            var enrollment = await db.Enrollments.AsNoTracking()
                .Include(e => e.Batch)
                .Where(e => e.StudentProfileId == profile.Id && e.Batch.CourseId == assignment.CourseId)
                .OrderByDescending(e => e.EnrolledAtUtc)
                .FirstOrDefaultAsync(ct);

            AssignmentSubmission? submission = null;
            if (enrollment is not null)
            {
                submission = await db.AssignmentSubmissions.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.AssignmentId == id && s.EnrollmentId == enrollment.Id, ct);
            }

            var dueAt = (enrollment?.EnrolledAtUtc ?? DateTime.UtcNow).AddDays(assignment.DueOffsetDays);

            var dto = new AssignmentDetailDto
            {
                Id = assignment.Id,
                EnrollmentId = enrollment?.Id,
                AssignmentNumber = assignment.AssignmentNumber,
                Title = assignment.Title,
                Description = assignment.Description,
                ResourcesUrl = assignment.ResourcesUrl,
                MaxScore = assignment.MaxScore,
                DueAtUtc = dueAt,
                CourseTitle = assignment.Course.Title,
                ToolName = assignment.CourseTool?.ToolName,
                SubmissionId = submission?.Id,
                Status = submission is null ? "NotStarted" : submission.Status.ToString(),
                SubmissionUrl = submission?.SubmissionUrl,
                SubmissionText = submission?.SubmissionText,
                SubmittedAtUtc = submission?.SubmittedAtUtc,
                Score = submission?.Score,
                Feedback = submission?.Feedback
            };

            return Results.Ok(ApiResponse<AssignmentDetailDto>.Ok(dto));
        }).WithName("StudentGetAssignment");

        group.MapPost("/assignments/{id:guid}/submit", async (
            Guid id,
            SubmitAssignmentRequest req,
            ICurrentUserService currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.SubmissionUrl) && string.IsNullOrWhiteSpace(req.SubmissionText))
                return Results.BadRequest(ApiResponse<object>.Fail("Provide a submission URL or some text"));

            var userId = currentUser.UserId;
            if (userId is null) return Results.Unauthorized();

            var profile = await db.StudentProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId.Value, ct);
            if (profile is null) return Results.BadRequest(ApiResponse<object>.Fail("No student profile"));

            var assignment = await db.Assignments.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id && a.IsPublished, ct);
            if (assignment is null) return Results.NotFound(ApiResponse<object>.Fail("Assignment not found"));

            var enrollment = await db.Enrollments.AsNoTracking()
                .Include(e => e.Batch)
                .Where(e => e.StudentProfileId == profile.Id && e.Batch.CourseId == assignment.CourseId)
                .OrderByDescending(e => e.EnrolledAtUtc)
                .FirstOrDefaultAsync(ct);
            if (enrollment is null) return Results.BadRequest(ApiResponse<object>.Fail("You're not enrolled in this course"));

            // Idempotent: if a submission exists in non-final state, update it; otherwise create
            var existing = await db.AssignmentSubmissions
                .FirstOrDefaultAsync(s => s.AssignmentId == id && s.EnrollmentId == enrollment.Id, ct);

            if (existing is null)
            {
                existing = new AssignmentSubmission
                {
                    AssignmentId = id,
                    EnrollmentId = enrollment.Id,
                    SubmissionUrl = req.SubmissionUrl,
                    SubmissionText = req.SubmissionText,
                    SubmittedAtUtc = DateTime.UtcNow,
                    Status = SubmissionStatus.Submitted
                };
                db.AssignmentSubmissions.Add(existing);
            }
            else
            {
                if (existing.Status == SubmissionStatus.Graded)
                    return Results.BadRequest(ApiResponse<object>.Fail("This submission has already been graded — contact your instructor to resubmit"));

                existing.SubmissionUrl = req.SubmissionUrl;
                existing.SubmissionText = req.SubmissionText;
                existing.SubmittedAtUtc = DateTime.UtcNow;
                existing.Status = SubmissionStatus.Submitted;
            }

            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<object>.Ok(new { submissionId = existing.Id, status = existing.Status.ToString() }, "Submitted"));
        }).WithName("StudentSubmitAssignment");

        // ============================================================
        // PROJECTS
        // ============================================================

        group.MapGet("/projects", async (
            ICurrentUserService currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null) return Results.Unauthorized();

            var profile = await db.StudentProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId.Value, ct);
            if (profile is null)
                return Results.Ok(ApiResponse<IReadOnlyList<ProjectListItemDto>>.Ok(Array.Empty<ProjectListItemDto>()));

            var enrollments = await db.Enrollments.AsNoTracking()
                .Include(e => e.Batch)
                .Where(e => e.StudentProfileId == profile.Id)
                .ToListAsync(ct);
            var courseIds = enrollments.Select(e => e.Batch.CourseId).Distinct().ToList();
            var enrollmentIds = enrollments.Select(e => e.Id).ToList();

            var projects = await db.Projects.AsNoTracking()
                .Include(p => p.Course)
                .Where(p => courseIds.Contains(p.CourseId))
                .OrderBy(p => p.ProjectNumber)
                .ToListAsync(ct);

            var submissions = await db.ProjectSubmissions.AsNoTracking()
                .Where(s => enrollmentIds.Contains(s.EnrollmentId))
                .ToListAsync(ct);

            var items = projects.Select(p =>
            {
                var sub = submissions.FirstOrDefault(s => s.ProjectId == p.Id);
                return new ProjectListItemDto
                {
                    Id = p.Id,
                    ProjectNumber = p.ProjectNumber,
                    Title = p.Title,
                    CourseTitle = p.Course.Title,
                    RecommendedDurationWeeks = p.RecommendedDurationWeeks,
                    SubmissionId = sub?.Id,
                    Status = sub is null ? "NotStarted" : sub.Status.ToString(),
                    Score = sub?.TotalScore
                };
            }).ToList();

            return Results.Ok(ApiResponse<IReadOnlyList<ProjectListItemDto>>.Ok(items));
        }).WithName("StudentListProjects");

        group.MapGet("/projects/{id:guid}", async (
            Guid id,
            ICurrentUserService currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null) return Results.Unauthorized();

            var profile = await db.StudentProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId.Value, ct);
            if (profile is null) return Results.NotFound(ApiResponse<ProjectDetailDto>.Fail("No student profile"));

            var project = await db.Projects.AsNoTracking()
                .Include(p => p.Course)
                .FirstOrDefaultAsync(p => p.Id == id, ct);
            if (project is null) return Results.NotFound(ApiResponse<ProjectDetailDto>.Fail("Project not found"));

            var enrollment = await db.Enrollments.AsNoTracking()
                .Include(e => e.Batch)
                .Where(e => e.StudentProfileId == profile.Id && e.Batch.CourseId == project.CourseId)
                .OrderByDescending(e => e.EnrolledAtUtc)
                .FirstOrDefaultAsync(ct);

            ProjectSubmission? submission = null;
            if (enrollment is not null)
            {
                submission = await db.ProjectSubmissions.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.ProjectId == id && s.EnrollmentId == enrollment.Id, ct);
            }

            var dto = new ProjectDetailDto
            {
                Id = project.Id,
                EnrollmentId = enrollment?.Id,
                ProjectNumber = project.ProjectNumber,
                Title = project.Title,
                Brief = project.Brief,
                DatasetUrl = project.DatasetUrl,
                RubricJson = project.RubricJson,
                RecommendedDurationWeeks = project.RecommendedDurationWeeks,
                CourseTitle = project.Course.Title,
                SubmissionId = submission?.Id,
                Status = submission is null ? "NotStarted" : submission.Status.ToString(),
                RepoUrl = submission?.RepoUrl,
                DeployedUrl = submission?.DeployedUrl,
                PresentationUrl = submission?.PresentationUrl,
                Description = submission?.Description,
                SubmittedAtUtc = submission?.SubmittedAtUtc,
                TotalScore = submission?.TotalScore,
                Feedback = submission?.Feedback,
                EvaluationJson = submission?.EvaluationJson
            };

            return Results.Ok(ApiResponse<ProjectDetailDto>.Ok(dto));
        }).WithName("StudentGetProject");

        group.MapPost("/projects/{id:guid}/submit", async (
            Guid id,
            SubmitProjectRequest req,
            ICurrentUserService currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.RepoUrl)
                && string.IsNullOrWhiteSpace(req.DeployedUrl)
                && string.IsNullOrWhiteSpace(req.PresentationUrl))
                return Results.BadRequest(ApiResponse<object>.Fail("Provide at least a repo, deploy, or presentation URL"));

            var userId = currentUser.UserId;
            if (userId is null) return Results.Unauthorized();

            var profile = await db.StudentProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId.Value, ct);
            if (profile is null) return Results.BadRequest(ApiResponse<object>.Fail("No student profile"));

            var project = await db.Projects.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id, ct);
            if (project is null) return Results.NotFound(ApiResponse<object>.Fail("Project not found"));

            var enrollment = await db.Enrollments.AsNoTracking()
                .Include(e => e.Batch)
                .Where(e => e.StudentProfileId == profile.Id && e.Batch.CourseId == project.CourseId)
                .OrderByDescending(e => e.EnrolledAtUtc)
                .FirstOrDefaultAsync(ct);
            if (enrollment is null) return Results.BadRequest(ApiResponse<object>.Fail("You're not enrolled in this course"));

            var existing = await db.ProjectSubmissions
                .FirstOrDefaultAsync(s => s.ProjectId == id && s.EnrollmentId == enrollment.Id, ct);

            if (existing is null)
            {
                existing = new ProjectSubmission
                {
                    ProjectId = id,
                    EnrollmentId = enrollment.Id,
                    RepoUrl = req.RepoUrl,
                    DeployedUrl = req.DeployedUrl,
                    PresentationUrl = req.PresentationUrl,
                    Description = req.Description,
                    SubmittedAtUtc = DateTime.UtcNow,
                    Status = SubmissionStatus.Submitted
                };
                db.ProjectSubmissions.Add(existing);
            }
            else
            {
                if (existing.Status == SubmissionStatus.Graded)
                    return Results.BadRequest(ApiResponse<object>.Fail("This submission has already been graded — contact your instructor to resubmit"));

                existing.RepoUrl = req.RepoUrl;
                existing.DeployedUrl = req.DeployedUrl;
                existing.PresentationUrl = req.PresentationUrl;
                existing.Description = req.Description;
                existing.SubmittedAtUtc = DateTime.UtcNow;
                existing.Status = SubmissionStatus.Submitted;
            }

            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<object>.Ok(new { submissionId = existing.Id, status = existing.Status.ToString() }, "Submitted"));
        }).WithName("StudentSubmitProject");

        // ============================================================
        // PROFILE
        // ============================================================

        group.MapGet("/profile", async (
            ICurrentUserService currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null) return Results.Unauthorized();

            var profile = await db.StudentProfiles.AsNoTracking()
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId.Value, ct);
            if (profile is null || profile.User is null)
                return Results.NotFound(ApiResponse<StudentProfileDto>.Fail("Profile not found"));

            return Results.Ok(ApiResponse<StudentProfileDto>.Ok(new StudentProfileDto
            {
                FullName = profile.User.FullName,
                Email = profile.User.Email ?? string.Empty,
                PhoneNumber = profile.User.PhoneNumber,
                City = profile.User.City,
                State = profile.User.State,
                ProfileImageUrl = profile.User.ProfileImageUrl,
                GraduationDegree = profile.GraduationDegree,
                GraduationYear = profile.GraduationYear,
                GraduationCollege = profile.GraduationCollege,
                CurrentRole = profile.CurrentRole,
                TargetRole = profile.TargetRole,
                LinkedinUrl = profile.LinkedinUrl,
                GithubUrl = profile.GithubUrl,
                PortfolioUrl = profile.PortfolioUrl,
                Bio = profile.Bio
            }));
        }).WithName("StudentGetProfile");

        group.MapPut("/profile", async (
            UpdateProfileRequest req,
            ICurrentUserService currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null) return Results.Unauthorized();

            var profile = await db.StudentProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId.Value, ct);
            if (profile is null || profile.User is null)
                return Results.NotFound(ApiResponse<object>.Fail("Profile not found"));

            // Update the AspNetUsers row
            profile.User.FullName = req.FullName;
            profile.User.PhoneNumber = req.PhoneNumber;
            profile.User.City = req.City;
            profile.User.State = req.State;
            profile.User.ProfileImageUrl = req.ProfileImageUrl;

            // Update the StudentProfile row
            profile.GraduationDegree = req.GraduationDegree;
            profile.GraduationYear = req.GraduationYear;
            profile.GraduationCollege = req.GraduationCollege;
            profile.CurrentRole = req.CurrentRole;
            profile.TargetRole = req.TargetRole;
            profile.LinkedinUrl = req.LinkedinUrl;
            profile.GithubUrl = req.GithubUrl;
            profile.PortfolioUrl = req.PortfolioUrl;
            profile.Bio = req.Bio;

            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<object>.Ok(new { ok = true }, "Profile updated"));
        }).WithName("StudentUpdateProfile");

        // ============================================================
        // NOTIFICATIONS
        // ============================================================

        group.MapGet("/mock-interviews", async (
            ICurrentUserService currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null) return Results.Unauthorized();

            var profile = await db.StudentProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId.Value, ct);
            if (profile is null)
                return Results.Ok(ApiResponse<IReadOnlyList<StudentMockInterviewDetailDto>>.Ok(new List<StudentMockInterviewDetailDto>()));

            var enrollmentIds = await db.Enrollments.AsNoTracking()
                .Where(e => e.StudentProfileId == profile.Id)
                .Select(e => e.Id)
                .ToListAsync(ct);

            var items = await db.MockInterviews.AsNoTracking()
                .Include(m => m.Enrollment).ThenInclude(e => e.Batch).ThenInclude(b => b.Course)
                .Where(m => enrollmentIds.Contains(m.EnrollmentId))
                .OrderByDescending(m => m.ScheduledAtUtc)
                .Select(m => new StudentMockInterviewDetailDto
                {
                    Id = m.Id,
                    CourseTitle = m.Enrollment.Batch.Course.Title,
                    RoundNumber = m.RoundNumber,
                    ScheduledAtUtc = m.ScheduledAtUtc,
                    DurationMinutes = m.DurationMinutes,
                    MeetingUrl = m.MeetingUrl,
                    Status = m.Status.ToString(),
                    TechnicalRating = m.TechnicalRating,
                    CommunicationRating = m.CommunicationRating,
                    ProblemSolvingRating = m.ProblemSolvingRating,
                    OverallRating = m.OverallRating,
                    Strengths = m.Strengths,
                    AreasToImprove = m.AreasToImprove,
                    RecordingUrl = m.RecordingUrl,
                    RecommendedNextSteps = m.RecommendedNextSteps
                })
                .ToListAsync(ct);

            return Results.Ok(ApiResponse<IReadOnlyList<StudentMockInterviewDetailDto>>.Ok(items));
        }).WithName("StudentMockInterviews");

        group.MapGet("/attendance", async (ICurrentUserService currentUser, AppDbContext db, CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null) return Results.Unauthorized();

            var profile = await db.StudentProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId.Value, ct);
            if (profile is null) return Results.Ok(ApiResponse<StudentAttendanceDto>.Ok(new StudentAttendanceDto()));

            var enrollmentIds = await db.Enrollments.AsNoTracking()
                .Where(e => e.StudentProfileId == profile.Id)
                .Select(e => e.Id).ToListAsync(ct);

            var atts = await db.SessionAttendances.AsNoTracking()
                .Include(a => a.Session).ThenInclude(s => s.Batch).ThenInclude(b => b.Course)
                .Where(a => enrollmentIds.Contains(a.EnrollmentId))
                .OrderByDescending(a => a.Session.ScheduledAtUtc)
                .ToListAsync(ct);

            var present = atts.Count(a => a.Status == AttendanceStatus.Present);
            var late = atts.Count(a => a.Status == AttendanceStatus.Late);
            var absent = atts.Count(a => a.Status == AttendanceStatus.Absent);
            var total = atts.Count;

            var dto = new StudentAttendanceDto
            {
                TotalSessions = total,
                Present = present,
                Late = late,
                Absent = absent,
                PercentPresent = total == 0 ? 0 : (int)Math.Round((present + late) * 100.0 / total),
                Items = atts.Select(a => new StudentAttendanceItemDto
                {
                    SessionTitle = a.Session.Title,
                    CourseTitle = a.Session.Batch.Course.Title,
                    ScheduledAtUtc = a.Session.ScheduledAtUtc,
                    Status = a.Status.ToString()
                }).ToList()
            };
            return Results.Ok(ApiResponse<StudentAttendanceDto>.Ok(dto));
        }).WithName("StudentAttendance");

        group.MapGet("/jobs", async (ICurrentUserService currentUser, AppDbContext db, CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null) return Results.Unauthorized();

            var profile = await db.StudentProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId.Value, ct);
            var appliedJobIds = profile is null
                ? new List<Guid>()
                : await db.JobReferrals.AsNoTracking().Where(r => r.StudentProfileId == profile.Id).Select(r => r.JobOpeningId).ToListAsync(ct);

            var jobs = await db.JobOpenings.AsNoTracking()
                .Where(j => j.Status == JobOpeningStatus.Open)
                .OrderByDescending(j => j.PostedAtUtc)
                .ToListAsync(ct);

            var dto = jobs.Select(j => new StudentJobDto
            {
                Id = j.Id,
                CompanyName = j.CompanyName,
                CompanyLogoUrl = j.CompanyLogoUrl,
                RoleTitle = j.RoleTitle,
                Description = j.Description,
                SkillsRequired = j.SkillsRequired,
                MinExperienceYears = j.MinExperienceYears,
                MinCtcLpa = j.MinCtcLpa,
                MaxCtcLpa = j.MaxCtcLpa,
                Location = j.Location,
                WorkMode = j.WorkMode,
                PostedAtUtc = j.PostedAtUtc,
                ApplicationUrl = j.ApplicationUrl,
                HasApplied = appliedJobIds.Contains(j.Id)
            }).ToList();

            return Results.Ok(ApiResponse<IReadOnlyList<StudentJobDto>>.Ok(dto));
        }).WithName("StudentJobs");

        group.MapPost("/jobs/{id:guid}/apply", async (Guid id, ICurrentUserService currentUser, AppDbContext db, CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null) return Results.Unauthorized();

            var profile = await db.StudentProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId.Value, ct);
            if (profile is null) return Results.BadRequest(ApiResponse<object>.Fail("No student profile."));

            var job = await db.JobOpenings.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id && j.Status == JobOpeningStatus.Open, ct);
            if (job is null) return Results.NotFound(ApiResponse<object>.Fail("Job not found or closed."));

            var already = await db.JobReferrals.AsNoTracking().AnyAsync(r => r.JobOpeningId == id && r.StudentProfileId == profile.Id, ct);
            if (already) return Results.Ok(ApiResponse<object>.Ok(new { id }, "You've already applied to this role."));

            db.JobReferrals.Add(new JobReferral
            {
                JobOpeningId = id,
                StudentProfileId = profile.Id,
                ReferredByUserId = userId.Value,
                ReferredAtUtc = DateTime.UtcNow,
                Status = JobReferralStatus.Referred
            });
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<object>.Ok(new { id }, "Application submitted."));
        }).WithName("StudentApplyJob");

        group.MapGet("/placements", async (ICurrentUserService currentUser, AppDbContext db, CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null) return Results.Unauthorized();

            var profile = await db.StudentProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId.Value, ct);
            if (profile is null) return Results.Ok(ApiResponse<IReadOnlyList<StudentPlacementDto>>.Ok(new List<StudentPlacementDto>()));

            var refs = await db.JobReferrals.AsNoTracking()
                .Include(r => r.JobOpening)
                .Where(r => r.StudentProfileId == profile.Id)
                .OrderByDescending(r => r.ReferredAtUtc)
                .ToListAsync(ct);

            var dto = refs.Select(r => new StudentPlacementDto
            {
                Id = r.Id,
                CompanyName = r.JobOpening.CompanyName,
                RoleTitle = r.JobOpening.RoleTitle,
                Status = r.Status.ToString(),
                ReferredAtUtc = r.ReferredAtUtc,
                StatusUpdatedAtUtc = r.StatusUpdatedAtUtc,
                OfferedCtcLpa = r.OfferedCtcLpa,
                JoiningDate = r.JoiningDate,
                Notes = r.Notes
            }).ToList();

            return Results.Ok(ApiResponse<IReadOnlyList<StudentPlacementDto>>.Ok(dto));
        }).WithName("StudentPlacements");

        group.MapPost("/mock-interviews/request", async (StudentMockInterviewRequest req, ICurrentUserService currentUser, AppDbContext db, CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null) return Results.Unauthorized();

            var profile = await db.StudentProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId.Value, ct);
            if (profile is null) return Results.BadRequest(ApiResponse<object>.Fail("No student profile."));

            var enrollment = await db.Enrollments.AsNoTracking()
                .Where(e => e.StudentProfileId == profile.Id && e.Status == SharedEnums.EnrollmentStatus.Active)
                .OrderByDescending(e => e.EnrolledAtUtc)
                .FirstOrDefaultAsync(ct);
            if (enrollment is null)
                return Results.BadRequest(ApiResponse<object>.Fail("You need an active enrollment to request a mock interview."));

            var round = (await db.MockInterviews.Where(m => m.EnrollmentId == enrollment.Id).MaxAsync(m => (int?)m.RoundNumber, ct) ?? 0) + 1;

            db.MockInterviews.Add(new MockInterview
            {
                EnrollmentId = enrollment.Id,
                RoundNumber = round,
                ScheduledAtUtc = req.PreferredAtUtc,
                Status = MockInterviewStatus.Scheduled
            });
            db.Notifications.Add(new Notification
            {
                UserId = userId.Value,
                Type = "MockInterviewRequested",
                Title = "Mock interview requested",
                Message = $"We received your request for {req.PreferredAtUtc:dd MMM yyyy, HH:mm} UTC. Our team will confirm shortly.",
                LinkUrl = "/student/mock-interviews",
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<object>.Ok(new { round }, "Mock interview requested."));
        }).WithName("StudentRequestMockInterview");

        group.MapGet("/resumes", async (ICurrentUserService currentUser, AppDbContext db, CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null) return Results.Unauthorized();

            var profile = await db.StudentProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId.Value, ct);
            if (profile is null) return Results.Ok(ApiResponse<IReadOnlyList<StudentResumeDto>>.Ok(new List<StudentResumeDto>()));

            var rows = await db.Resumes.AsNoTracking()
                .Where(r => r.StudentProfileId == profile.Id)
                .OrderByDescending(r => r.Version)
                .Select(r => new StudentResumeDto
                {
                    Id = r.Id,
                    Version = r.Version,
                    FileUrl = r.FileUrl,
                    Status = r.Status.ToString(),
                    IsCurrent = r.IsCurrent,
                    CreatedAtUtc = r.CreatedAtUtc,
                    ReviewerNotes = r.ReviewerNotes,
                    ReviewedAtUtc = r.ReviewedAtUtc
                })
                .ToListAsync(ct);

            return Results.Ok(ApiResponse<IReadOnlyList<StudentResumeDto>>.Ok(rows));
        }).WithName("StudentResumes");

        group.MapPost("/resumes", async (
            IFormFile file,
            ICurrentUserService currentUser,
            AppDbContext db,
            IFileStorage storage,
            CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null) return Results.Unauthorized();

            if (file is null || file.Length == 0)
                return Results.BadRequest(ApiResponse<object>.Fail("Please choose a file to upload."));

            const long maxBytes = 5 * 1024 * 1024;
            if (file.Length > maxBytes)
                return Results.BadRequest(ApiResponse<object>.Fail("File is too large (max 5 MB)."));

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext is not (".pdf" or ".doc" or ".docx"))
                return Results.BadRequest(ApiResponse<object>.Fail("Only PDF or Word (.doc/.docx) files are allowed."));

            var profile = await db.StudentProfiles.FirstOrDefaultAsync(p => p.UserId == userId.Value, ct);
            if (profile is null) return Results.BadRequest(ApiResponse<object>.Fail("No student profile."));

            string url;
            await using (var stream = file.OpenReadStream())
                url = await storage.UploadAsync(stream, file.FileName, file.ContentType, ct);

            var maxVersion = await db.Resumes.Where(r => r.StudentProfileId == profile.Id).MaxAsync(r => (int?)r.Version, ct) ?? 0;

            var currents = await db.Resumes.Where(r => r.StudentProfileId == profile.Id && r.IsCurrent).ToListAsync(ct);
            foreach (var c in currents) c.IsCurrent = false;

            db.Resumes.Add(new Resume
            {
                StudentProfileId = profile.Id,
                Version = maxVersion + 1,
                FileUrl = url,
                Status = ResumeStatus.UnderReview,
                IsCurrent = true
            });
            db.Notifications.Add(new Notification
            {
                UserId = userId.Value,
                Type = "ResumeSubmitted",
                Title = "Resume submitted",
                Message = "Your resume is now under review by our placement team.",
                LinkUrl = "/student/resume",
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync(ct);

            return Results.Ok(ApiResponse<object>.Ok(new { version = maxVersion + 1 }, "Resume uploaded and sent for review."));
        }).WithName("StudentUploadResume").DisableAntiforgery();

        group.MapGet("/notifications", async (
            ICurrentUserService currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null) return Results.Unauthorized();

            var rows = await db.Notifications.AsNoTracking()
                .Where(n => n.UserId == userId.Value)
                .OrderByDescending(n => n.CreatedAtUtc)
                .Take(100)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Type = n.Type,
                    Title = n.Title,
                    Message = n.Message,
                    LinkUrl = n.LinkUrl,
                    IsRead = n.ReadAtUtc != null,
                    CreatedAtUtc = n.CreatedAtUtc,
                    ReadAtUtc = n.ReadAtUtc
                })
                .ToListAsync(ct);

            return Results.Ok(ApiResponse<IReadOnlyList<NotificationDto>>.Ok(rows));
        }).WithName("StudentListNotifications");

        group.MapPost("/notifications/{id:guid}/read", async (
            Guid id,
            ICurrentUserService currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null) return Results.Unauthorized();

            var n = await db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId.Value, ct);
            if (n is null) return Results.NotFound();
            if (n.ReadAtUtc is null)
            {
                n.ReadAtUtc = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }
            return Results.Ok(ApiResponse<object>.Ok(new { id = n.Id, readAt = n.ReadAtUtc }));
        }).WithName("StudentMarkNotificationRead");

        group.MapPost("/notifications/read-all", async (
            ICurrentUserService currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null) return Results.Unauthorized();

            var unread = await db.Notifications.Where(n => n.UserId == userId.Value && n.ReadAtUtc == null).ToListAsync(ct);
            var now = DateTime.UtcNow;
            foreach (var n in unread) n.ReadAtUtc = now;
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<object>.Ok(new { count = unread.Count }));
        }).WithName("StudentMarkAllNotificationsRead");

        return routes;
    }
}
