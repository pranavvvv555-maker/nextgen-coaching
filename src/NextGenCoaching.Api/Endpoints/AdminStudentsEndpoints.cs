using Microsoft.EntityFrameworkCore;
using NextGenCoaching.Core.Enums;
using NextGenCoaching.Core.Persistence;
using NextGenCoaching.Shared.Constants;
using NextGenCoaching.Shared.Dtos;
using NextGenCoaching.Shared.Requests;
using NextGenCoaching.Shared.Responses;

namespace NextGenCoaching.Api.Endpoints;

public static class AdminStudentsEndpoints
{
    public static IEndpointRouteBuilder MapAdminStudentsEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(ApiRoutes.Admin.Students)
            .WithTags("Admin · Students")
            .RequireAuthorization("RequireCounsellor");

        // GET paged + filterable list of students
        group.MapGet("/", async (
            AppDbContext db,
            CancellationToken ct,
            int Page = 1, int PageSize = 25, string? Status = null, string? Search = null) =>
        {
            var q = new StudentListQuery { Page = Page, PageSize = PageSize, Status = Status, Search = Search };
            var page = Math.Max(1, q.Page);
            var size = Math.Clamp(q.PageSize, 1, 100);

            var query = db.StudentProfiles.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q.Status) && Enum.TryParse<StudentStatus>(q.Status, true, out var st))
                query = query.Where(p => p.Status == st);

            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                var s = q.Search.Trim();
                query = query.Where(p => p.User != null && (
                    EF.Functions.ILike(p.User.FullName, $"%{s}%") ||
                    (p.User.Email != null && EF.Functions.ILike(p.User.Email, $"%{s}%")) ||
                    (p.User.PhoneNumber != null && EF.Functions.ILike(p.User.PhoneNumber, $"%{s}%"))));
            }

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(p => p.CreatedAtUtc)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(p => new AdminStudentListItemDto
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    FullName = p.User != null ? p.User.FullName : "",
                    Email = p.User != null ? (p.User.Email ?? "") : "",
                    Phone = p.User != null ? p.User.PhoneNumber : null,
                    City = p.User != null ? p.User.City : null,
                    Status = p.Status.ToString(),
                    EnrollmentCount = p.Enrollments.Count,
                    LatestCourseTitle = p.Enrollments
                        .OrderByDescending(e => e.EnrolledAtUtc)
                        .Select(e => e.Batch.Course.Title)
                        .FirstOrDefault(),
                    LastLoginAtUtc = p.User != null ? p.User.LastLoginAtUtc : null,
                    CreatedAtUtc = p.CreatedAtUtc
                })
                .ToListAsync(ct);

            return Results.Ok(ApiResponse<PagedResult<AdminStudentListItemDto>>.Ok(new PagedResult<AdminStudentListItemDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = size
            }));
        }).WithName("AdminListStudents");

        // GET single student with profile, enrolments, and payments
        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var p = await db.StudentProfiles.AsNoTracking()
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (p is null)
                return Results.NotFound(ApiResponse<AdminStudentDetailDto>.Fail("Student not found"));

            var enrollments = await db.Enrollments.AsNoTracking()
                .Where(e => e.StudentProfileId == id)
                .OrderByDescending(e => e.EnrolledAtUtc)
                .Select(e => new AdminStudentEnrollmentDto
                {
                    EnrollmentId = e.Id,
                    CourseTitle = e.Batch.Course.Title,
                    BatchName = e.Batch.Name,
                    Status = e.Status.ToString(),
                    AmountPaid = e.AmountPaid,
                    CompletionPercentage = e.CompletionPercentage,
                    EnrolledAtUtc = e.EnrolledAtUtc
                })
                .ToListAsync(ct);

            var payments = await db.PaymentTransactions.AsNoTracking()
                .Where(t => t.StudentProfileId == id)
                .OrderByDescending(t => t.CreatedAtUtc)
                .Select(t => new AdminStudentPaymentDto
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    Currency = t.Currency,
                    Status = t.Status.ToString(),
                    CourseTitle = t.Course != null ? t.Course.Title : null,
                    CreatedAtUtc = t.CreatedAtUtc
                })
                .ToListAsync(ct);

            var resumeCount = await db.Resumes.CountAsync(r => r.StudentProfileId == id, ct);

            var dto = new AdminStudentDetailDto
            {
                Id = p.Id,
                UserId = p.UserId,
                FullName = p.User?.FullName ?? "",
                Email = p.User?.Email ?? "",
                Phone = p.User?.PhoneNumber,
                City = p.User?.City,
                State = p.User?.State,
                Status = p.Status.ToString(),
                CreatedAtUtc = p.CreatedAtUtc,
                LastLoginAtUtc = p.User?.LastLoginAtUtc,
                GraduationDegree = p.GraduationDegree,
                GraduationYear = p.GraduationYear,
                GraduationCollege = p.GraduationCollege,
                CurrentRole = p.CurrentRole,
                TargetRole = p.TargetRole,
                LinkedinUrl = p.LinkedinUrl,
                GithubUrl = p.GithubUrl,
                PortfolioUrl = p.PortfolioUrl,
                Bio = p.Bio,
                ResumeCount = resumeCount,
                TotalPaid = enrollments.Sum(e => e.AmountPaid),
                Enrollments = enrollments,
                Payments = payments
            };

            return Results.Ok(ApiResponse<AdminStudentDetailDto>.Ok(dto));
        }).WithName("AdminGetStudent");

        return routes;
    }
}
