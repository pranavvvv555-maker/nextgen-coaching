using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using NextGenCoaching.Core.Entities;
using NextGenCoaching.Core.Enums;
using NextGenCoaching.Core.Interfaces;
using NextGenCoaching.Core.Persistence;
using NextGenCoaching.Core.Services.Email;
using NextGenCoaching.Shared.Constants;
using NextGenCoaching.Shared.Dtos;
using NextGenCoaching.Shared.Requests;
using NextGenCoaching.Shared.Responses;

namespace NextGenCoaching.Api.Endpoints;

public static class HrEndpoints
{
    public static IEndpointRouteBuilder MapHrEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(ApiRoutes.Base + "/hr")
            .WithTags("HR")
            .RequireAuthorization("RequireHrReviewer");

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

            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId.Value, ct);
            var now = DateTime.UtcNow;
            var yearStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var pendingCount  = await db.Resumes.AsNoTracking().CountAsync(r => r.Status == ResumeStatus.UnderReview, ct);
            var openJobsCount = await db.JobOpenings.AsNoTracking().CountAsync(j => j.Status == JobOpeningStatus.Open, ct);
            var activeReferrals = await db.JobReferrals.AsNoTracking()
                .CountAsync(r => r.Status == JobReferralStatus.Referred
                                  || r.Status == JobReferralStatus.Screening
                                  || r.Status == JobReferralStatus.Interviewing
                                  || r.Status == JobReferralStatus.Offered, ct);
            var placedThisYear = await db.JobReferrals.AsNoTracking()
                .CountAsync(r => r.Status == JobReferralStatus.Joined
                                  && r.StatusUpdatedAtUtc != null
                                  && r.StatusUpdatedAtUtc >= yearStart, ct);

            var recentResumes = await db.Resumes.AsNoTracking()
                .Include(r => r.StudentProfile).ThenInclude(p => p.User)
                .Where(r => r.Status == ResumeStatus.UnderReview)
                .OrderBy(r => r.CreatedAtUtc).Take(5)
                .Select(r => new HrResumeListItemDto
                {
                    Id = r.Id,
                    StudentProfileId = r.StudentProfileId,
                    Version = r.Version,
                    StudentName = r.StudentProfile.User != null ? r.StudentProfile.User.FullName : "—",
                    StudentEmail = r.StudentProfile.User != null ? (r.StudentProfile.User.Email ?? string.Empty) : string.Empty,
                    TargetRole = r.StudentProfile.TargetRole,
                    Status = r.Status.ToString(),
                    CreatedAtUtc = r.CreatedAtUtc,
                    AgeHours = (int)(now - r.CreatedAtUtc).TotalHours
                })
                .ToListAsync(ct);

            var recentJobs = await db.JobOpenings.AsNoTracking()
                .Include(j => j.Referrals)
                .Where(j => j.Status == JobOpeningStatus.Open)
                .OrderByDescending(j => j.PostedAtUtc).Take(5)
                .Select(j => new HrJobOpeningDto
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
                    ExpiresAtUtc = j.ExpiresAtUtc,
                    ApplicationUrl = j.ApplicationUrl,
                    Status = j.Status.ToString(),
                    ReferralsCount = j.Referrals.Count,
                    PlacedCount = j.Referrals.Count(r => r.Status == JobReferralStatus.Joined)
                })
                .ToListAsync(ct);

            return Results.Ok(ApiResponse<HrDashboardDto>.Ok(new HrDashboardDto
            {
                FullName = user?.FullName ?? "Reviewer",
                Stats = new HrStatsDto
                {
                    ResumesUnderReview = pendingCount,
                    OpenJobs = openJobsCount,
                    ActiveReferrals = activeReferrals,
                    StudentsPlacedThisYear = placedThisYear
                },
                RecentPendingResumes = recentResumes,
                RecentJobs = recentJobs
            }));
        }).WithName("HrDashboard");

        // ============================================================
        // RESUMES
        // ============================================================

        group.MapGet("/resumes/pending", async (AppDbContext db, CancellationToken ct) =>
        {
            var now = DateTime.UtcNow;
            var rows = await db.Resumes.AsNoTracking()
                .Include(r => r.StudentProfile).ThenInclude(p => p.User)
                .Where(r => r.Status == ResumeStatus.UnderReview)
                .OrderBy(r => r.CreatedAtUtc)
                .Select(r => new HrResumeListItemDto
                {
                    Id = r.Id,
                    StudentProfileId = r.StudentProfileId,
                    Version = r.Version,
                    StudentName = r.StudentProfile.User != null ? r.StudentProfile.User.FullName : "—",
                    StudentEmail = r.StudentProfile.User != null ? (r.StudentProfile.User.Email ?? string.Empty) : string.Empty,
                    TargetRole = r.StudentProfile.TargetRole,
                    Status = r.Status.ToString(),
                    CreatedAtUtc = r.CreatedAtUtc,
                    AgeHours = (int)(now - r.CreatedAtUtc).TotalHours
                })
                .ToListAsync(ct);
            return Results.Ok(ApiResponse<IReadOnlyList<HrResumeListItemDto>>.Ok(rows));
        }).WithName("HrListPendingResumes");

        group.MapGet("/resumes/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var r = await db.Resumes.AsNoTracking()
                .Include(x => x.StudentProfile).ThenInclude(p => p.User)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (r is null) return Results.NotFound(ApiResponse<HrResumeDetailDto>.Fail("Resume not found"));

            var dto = new HrResumeDetailDto
            {
                Id = r.Id,
                StudentProfileId = r.StudentProfileId,
                Version = r.Version,
                FileUrl = r.FileUrl,
                Status = r.Status.ToString(),
                StudentName = r.StudentProfile.User?.FullName ?? "—",
                StudentEmail = r.StudentProfile.User?.Email ?? string.Empty,
                StudentPhone = r.StudentProfile.User?.PhoneNumber,
                TargetRole = r.StudentProfile.TargetRole,
                CurrentRole = r.StudentProfile.CurrentRole,
                Bio = r.StudentProfile.Bio,
                LinkedinUrl = r.StudentProfile.LinkedinUrl,
                GithubUrl = r.StudentProfile.GithubUrl,
                PortfolioUrl = r.StudentProfile.PortfolioUrl,
                GraduationDegree = r.StudentProfile.GraduationDegree,
                GraduationYear = r.StudentProfile.GraduationYear,
                GraduationCollege = r.StudentProfile.GraduationCollege,
                ReviewerNotes = r.ReviewerNotes,
                ReviewedAtUtc = r.ReviewedAtUtc,
                CreatedAtUtc = r.CreatedAtUtc
            };
            return Results.Ok(ApiResponse<HrResumeDetailDto>.Ok(dto));
        }).WithName("HrGetResume");

        group.MapPost("/resumes/{id:guid}/review", async (
            Guid id,
            ReviewResumeRequest req,
            ICurrentUserService currentUser,
            AppDbContext db,
            IEmailDispatcher dispatcher,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<ResumeStatus>(req.Status, true, out var newStatus)
                || (newStatus != ResumeStatus.Approved && newStatus != ResumeStatus.Reviewed))
                return Results.BadRequest(ApiResponse<object>.Fail("Status must be 'Approved' or 'Reviewed'"));

            var r = await db.Resumes
                .Include(x => x.StudentProfile).ThenInclude(sp => sp!.User)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (r is null) return Results.NotFound(ApiResponse<object>.Fail("Resume not found"));

            r.Status = newStatus;
            r.ReviewerNotes = req.Notes;
            r.ReviewedAtUtc = DateTime.UtcNow;
            r.ReviewerUserId = currentUser.UserId;

            // Notify the student
            var studentUserId = r.StudentProfile?.UserId;
            if (studentUserId is not null)
            {
                var verb = newStatus == ResumeStatus.Approved ? "approved" : "reviewed";
                db.Notifications.Add(new Notification
                {
                    UserId = studentUserId.Value,
                    Type = newStatus == ResumeStatus.Approved ? "ResumeApproved" : "ResumeReviewed",
                    Title = $"Your resume v{r.Version} has been {verb}",
                    Message = newStatus == ResumeStatus.Approved
                        ? "Your resume is ready to share with hiring partners."
                        : (req.Notes is { Length: > 0 } ? "Please review the feedback and upload a new version." : "Please review the feedback."),
                    LinkUrl = "/student/profile",
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync(ct);

            var rEmail = r.StudentProfile?.User?.Email;
            if (!string.IsNullOrEmpty(rEmail))
            {
                await dispatcher.SendAsync("resume_reviewed", rEmail, new Dictionary<string, string?>
                {
                    ["full_name"] = r.StudentProfile?.User?.FullName,
                    ["status"] = newStatus == ResumeStatus.Approved ? "approved" : "reviewed",
                    ["notes"] = string.IsNullOrWhiteSpace(req.Notes) ? "" : $"Reviewer notes: {req.Notes}"
                }, ct);
            }

            return Results.Ok(ApiResponse<object>.Ok(new { r.Id, status = r.Status.ToString() }, "Resume reviewed"));
        }).WithName("HrReviewResume");

        // ============================================================
        // JOB OPENINGS
        // ============================================================

        group.MapGet("/jobs", async (
            AppDbContext db,
            string? status = null,
            CancellationToken ct = default) =>
        {
            var q = db.JobOpenings.AsNoTracking()
                .Include(j => j.Referrals)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<JobOpeningStatus>(status, true, out var st))
                q = q.Where(j => j.Status == st);

            var rows = await q.OrderByDescending(j => j.PostedAtUtc)
                .Select(j => new HrJobOpeningDto
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
                    ExpiresAtUtc = j.ExpiresAtUtc,
                    ApplicationUrl = j.ApplicationUrl,
                    Status = j.Status.ToString(),
                    ReferralsCount = j.Referrals.Count,
                    PlacedCount = j.Referrals.Count(r => r.Status == JobReferralStatus.Joined)
                })
                .ToListAsync(ct);

            return Results.Ok(ApiResponse<IReadOnlyList<HrJobOpeningDto>>.Ok(rows));
        }).WithName("HrListJobs");

        group.MapPost("/jobs", async (
            UpsertJobOpeningRequest req,
            ICurrentUserService currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var j = new JobOpening
            {
                CompanyName = req.CompanyName,
                CompanyLogoUrl = req.CompanyLogoUrl,
                RoleTitle = req.RoleTitle,
                Description = req.Description,
                SkillsRequired = req.SkillsRequired ?? new(),
                MinExperienceYears = req.MinExperienceYears,
                MinCtcLpa = req.MinCtcLpa,
                MaxCtcLpa = req.MaxCtcLpa,
                Location = req.Location,
                WorkMode = req.WorkMode,
                PostedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = req.ExpiresAtUtc?.ToUniversalTime(),
                ApplicationUrl = req.ApplicationUrl,
                PostedByUserId = currentUser.UserId,
                Status = JobOpeningStatus.Open
            };
            db.JobOpenings.Add(j);
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<Guid>.Ok(j.Id, "Job opening posted"));
        }).WithName("HrCreateJob");

        group.MapPut("/jobs/{id:guid}", async (
            Guid id,
            UpsertJobOpeningRequest req,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var j = await db.JobOpenings.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (j is null) return Results.NotFound(ApiResponse<object>.Fail("Job opening not found"));

            j.CompanyName = req.CompanyName;
            j.CompanyLogoUrl = req.CompanyLogoUrl;
            j.RoleTitle = req.RoleTitle;
            j.Description = req.Description;
            j.SkillsRequired = req.SkillsRequired ?? new();
            j.MinExperienceYears = req.MinExperienceYears;
            j.MinCtcLpa = req.MinCtcLpa;
            j.MaxCtcLpa = req.MaxCtcLpa;
            j.Location = req.Location;
            j.WorkMode = req.WorkMode;
            j.ExpiresAtUtc = req.ExpiresAtUtc?.ToUniversalTime();
            j.ApplicationUrl = req.ApplicationUrl;
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<Guid>.Ok(j.Id, "Updated"));
        }).WithName("HrUpdateJob");

        group.MapPost("/jobs/{id:guid}/close", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var j = await db.JobOpenings.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (j is null) return Results.NotFound(ApiResponse<object>.Fail("Job opening not found"));
            j.Status = JobOpeningStatus.Closed;
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<object>.Ok(new { id }, "Job closed"));
        }).WithName("HrCloseJob");

        // ============================================================
        // REFERRALS
        // ============================================================

        group.MapGet("/referrals", async (
            AppDbContext db,
            string? status = null,
            CancellationToken ct = default) =>
        {
            var q = db.JobReferrals.AsNoTracking()
                .Include(r => r.JobOpening)
                .Include(r => r.StudentProfile).ThenInclude(p => p.User)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<JobReferralStatus>(status, true, out var st))
                q = q.Where(r => r.Status == st);

            var rows = await q.OrderByDescending(r => r.ReferredAtUtc)
                .Select(r => new HrReferralDto
                {
                    Id = r.Id,
                    JobOpeningId = r.JobOpeningId,
                    CompanyName = r.JobOpening.CompanyName,
                    RoleTitle = r.JobOpening.RoleTitle,
                    StudentProfileId = r.StudentProfileId,
                    StudentName = r.StudentProfile.User != null ? r.StudentProfile.User.FullName : "—",
                    StudentEmail = r.StudentProfile.User != null ? (r.StudentProfile.User.Email ?? string.Empty) : string.Empty,
                    Status = r.Status.ToString(),
                    ReferredAtUtc = r.ReferredAtUtc,
                    StatusUpdatedAtUtc = r.StatusUpdatedAtUtc,
                    OfferedCtcLpa = r.OfferedCtcLpa,
                    JoiningDate = r.JoiningDate,
                    Notes = r.Notes
                })
                .ToListAsync(ct);

            return Results.Ok(ApiResponse<IReadOnlyList<HrReferralDto>>.Ok(rows));
        }).WithName("HrListReferrals");

        group.MapPost("/referrals/{id:guid}/status", async (
            Guid id,
            UpdateReferralStatusRequest req,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<JobReferralStatus>(req.Status, true, out var newStatus))
                return Results.BadRequest(ApiResponse<object>.Fail($"Invalid referral status '{req.Status}'"));

            var r = await db.JobReferrals.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (r is null) return Results.NotFound(ApiResponse<object>.Fail("Referral not found"));

            r.Status = newStatus;
            r.StatusUpdatedAtUtc = DateTime.UtcNow;
            if (req.OfferedCtcLpa.HasValue) r.OfferedCtcLpa = req.OfferedCtcLpa;
            if (req.JoiningDate.HasValue)   r.JoiningDate   = req.JoiningDate;
            if (!string.IsNullOrWhiteSpace(req.Notes)) r.Notes = req.Notes;

            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<object>.Ok(new { r.Id, status = r.Status.ToString() }, "Referral updated"));
        }).WithName("HrUpdateReferralStatus");

        // ---------- My profile (account fields) ----------
        group.MapGet("/profile", async (ICurrentUserService currentUser, AppDbContext db, CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null) return Results.Unauthorized();
            var u = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId.Value, ct);
            if (u is null) return Results.NotFound(ApiResponse<HrProfileDto>.Fail("Account not found"));
            return Results.Ok(ApiResponse<HrProfileDto>.Ok(new HrProfileDto
            {
                FullName = u.FullName,
                Email = u.Email ?? string.Empty,
                Phone = u.PhoneNumber,
                City = u.City
            }));
        }).WithName("HrGetProfile");

        group.MapPut("/profile", async (UpdateAccountRequest req, ICurrentUserService currentUser, AppDbContext db, CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null) return Results.Unauthorized();
            var u = await db.Users.FirstOrDefaultAsync(x => x.Id == userId.Value, ct);
            if (u is null) return Results.NotFound(ApiResponse<object>.Fail("Account not found"));
            if (!string.IsNullOrWhiteSpace(req.FullName)) u.FullName = req.FullName.Trim();
            u.PhoneNumber = req.Phone?.Trim();
            u.City = req.City;
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<object>.Ok(new { u.Id }, "Profile updated."));
        }).WithName("HrUpdateProfile");

        return routes;
    }
}
