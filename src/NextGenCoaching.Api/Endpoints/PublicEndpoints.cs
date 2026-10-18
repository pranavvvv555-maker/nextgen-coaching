using Microsoft.EntityFrameworkCore;
using NextGenCoaching.Core.Entities;
using NextGenCoaching.Core.Interfaces;
using NextGenCoaching.Core.Persistence;
using NextGenCoaching.Core.Services.Email;
using NextGenCoaching.Shared.Constants;
using NextGenCoaching.Shared.Dtos;
using NextGenCoaching.Shared.Enums;
using NextGenCoaching.Shared.Requests;
using NextGenCoaching.Shared.Responses;

namespace NextGenCoaching.Api.Endpoints;

public static class PublicEndpoints
{
    public static IEndpointRouteBuilder MapPublicEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(ApiRoutes.Base + "/public")
            .WithTags("Public")
            .AllowAnonymous();

        // ---------- Courses ----------
        group.MapGet("/courses", async (AppDbContext db, string? category, CancellationToken ct) =>
        {
            var q = db.Courses.AsNoTracking().Where(c => c.IsPublished);
            if (!string.IsNullOrWhiteSpace(category)) q = q.Where(c => c.Category == category);

            var list = await q.OrderBy(c => c.SortOrder)
                .Select(c => new CourseSummaryDto
                {
                    Id = c.Id,
                    Slug = c.Slug,
                    Title = c.Title,
                    Tagline = c.Tagline,
                    DurationMonths = c.DurationMonths,
                    Price = c.Price,
                    DiscountPrice = c.DiscountPrice,
                    Currency = c.Currency,
                    Mode = c.Mode,
                    Level = c.Level,
                    HeroImageUrl = c.HeroImageUrl,
                    IsFlagship = c.IsFlagship,
                    Category = c.Category,
                    SortOrder = c.SortOrder
                }).ToListAsync(ct);

            return Results.Ok(ApiResponse<IReadOnlyList<CourseSummaryDto>>.Ok(list));
        }).WithName("GetCourses");

        group.MapGet("/courses/categories", async (AppDbContext db, CancellationToken ct) =>
        {
            var cats = await db.Courses.AsNoTracking()
                .Where(c => c.IsPublished && c.Category != null)
                .Select(c => c.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync(ct);
            return Results.Ok(ApiResponse<IReadOnlyList<string>>.Ok(cats));
        }).WithName("GetCourseCategories");

        group.MapGet("/courses/{slug}", async (string slug, AppDbContext db, CancellationToken ct) =>
        {
            var course = await db.Courses.AsNoTracking()
                .Include(c => c.JourneySteps.OrderBy(s => s.StepNumber))
                .Include(c => c.Tools.OrderBy(t => t.SortOrder))
                .Include(c => c.Features.OrderBy(f => f.SortOrder))
                .FirstOrDefaultAsync(c => c.Slug == slug && c.IsPublished, ct);

            if (course is null)
                return Results.NotFound(ApiResponse<CourseDetailDto>.Fail($"Course '{slug}' not found"));

            var dto = new CourseDetailDto
            {
                Id = course.Id,
                Slug = course.Slug,
                Title = course.Title,
                Tagline = course.Tagline,
                Description = course.Description,
                DurationMonths = course.DurationMonths,
                TotalHours = course.TotalHours,
                Price = course.Price,
                DiscountPrice = course.DiscountPrice,
                Currency = course.Currency,
                Mode = course.Mode,
                Level = course.Level,
                HeroImageUrl = course.HeroImageUrl,
                BrochureUrl = course.BrochureUrl,
                IsFlagship = course.IsFlagship,
                Category = course.Category,
                SortOrder = course.SortOrder,
                SeoTitle = course.SeoTitle,
                SeoDescription = course.SeoDescription,
                JourneySteps = course.JourneySteps
                    .Select(s => new CourseJourneyStepDto { StepNumber = s.StepNumber, Title = s.Title, Description = s.Description, Icon = s.Icon })
                    .ToList(),
                Tools = course.Tools
                    .Select(t => new CourseToolDto { ToolName = t.ToolName, IconUrl = t.IconUrl, Description = t.Description, SortOrder = t.SortOrder })
                    .ToList(),
                Features = course.Features
                    .Select(f => new CourseFeatureDto { Label = f.Label, Icon = f.Icon, SortOrder = f.SortOrder })
                    .ToList()
            };

            return Results.Ok(ApiResponse<CourseDetailDto>.Ok(dto));
        }).WithName("GetCourseBySlug");

        // ---------- Testimonials ----------
        group.MapGet("/testimonials/featured", async (AppDbContext db, CancellationToken ct) =>
        {
            var items = await db.Testimonials.AsNoTracking()
                .Where(t => t.IsFeatured)
                .Include(t => t.Course)
                .OrderBy(t => t.SortOrder)
                .Select(t => new TestimonialDto
                {
                    Id = t.Id,
                    StudentName = t.StudentName,
                    PhotoUrl = t.PhotoUrl,
                    CurrentRole = t.CurrentRole,
                    CurrentCompany = t.CurrentCompany,
                    CourseTitle = t.Course != null ? t.Course.Title : null,
                    BatchName = t.BatchName,
                    Quote = t.Quote,
                    VideoUrl = t.VideoUrl
                })
                .ToListAsync(ct);
            return Results.Ok(ApiResponse<IReadOnlyList<TestimonialDto>>.Ok(items));
        }).WithName("GetFeaturedTestimonials");

        // ---------- FAQs ----------
        group.MapGet("/faqs", async (AppDbContext db, CancellationToken ct) =>
        {
            var items = await db.Faqs.AsNoTracking()
                .Where(f => f.IsPublished)
                .OrderBy(f => f.Category).ThenBy(f => f.SortOrder)
                .Select(f => new FaqDto { Id = f.Id, Category = f.Category, Question = f.Question, Answer = f.Answer, SortOrder = f.SortOrder })
                .ToListAsync(ct);
            return Results.Ok(ApiResponse<IReadOnlyList<FaqDto>>.Ok(items));
        }).WithName("GetFaqs");

        // ---------- Webinars ----------
        group.MapGet("/webinars/upcoming", async (AppDbContext db, int? take, CancellationToken ct) =>
        {
            var n = Math.Clamp(take ?? 6, 1, 20);
            var items = await db.Webinars.AsNoTracking()
                .Include(w => w.Course)
                .Where(w => w.Status == WebinarStatus.Scheduled && w.ScheduledAtUtc > DateTime.UtcNow)
                .OrderBy(w => w.ScheduledAtUtc).Take(n)
                .Select(w => new WebinarDto
                {
                    Id = w.Id,
                    Title = w.Title,
                    Description = w.Description,
                    CourseId = w.CourseId,
                    CourseTitle = w.Course != null ? w.Course.Title : null,
                    ScheduledAtUtc = w.ScheduledAtUtc,
                    DurationMinutes = w.DurationMinutes,
                    BannerImageUrl = w.BannerImageUrl,
                    Status = w.Status.ToString()
                })
                .ToListAsync(ct);
            return Results.Ok(ApiResponse<IReadOnlyList<WebinarDto>>.Ok(items));
        }).WithName("GetUpcomingWebinars");

        group.MapPost("/webinars/{id:guid}/register", async (
            Guid id,
            RegisterWebinarRequest req,
            AppDbContext db,
            ILogger<Program> logger,
            IEmailDispatcher dispatcher,
            ICaptchaVerifier captcha,
            HttpContext http,
            CancellationToken ct) =>
        {
            if (!await captcha.VerifyAsync(req.CaptchaToken, http.Connection.RemoteIpAddress?.ToString(), ct))
                return Results.BadRequest(ApiResponse<object>.Fail("Captcha verification failed. Please try again."));

            return await db.Database.CreateExecutionStrategy().ExecuteAsync<IResult>(async () =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync(ct);
                // Serialize registrations for this session so capacity is respected under concurrency.
                var webinar = await db.Webinars.FromSqlInterpolated($"SELECT * FROM webinars WHERE id = {id} FOR UPDATE").FirstOrDefaultAsync(ct);
                if (webinar is null)
                    return Results.NotFound(ApiResponse<object>.Fail("Webinar not found"));
                if (webinar.Status != WebinarStatus.Scheduled || webinar.ScheduledAtUtc <= DateTime.UtcNow)
                    return Results.BadRequest(ApiResponse<object>.Fail("Webinar is not open for registration"));

                req.Email = req.Email.Trim().ToLowerInvariant();
                req.Phone = req.Phone.Trim();
                req.FullName = req.FullName.Trim();

                // Find or create lead (dedupe by email + phone)
                var lead = await db.Leads.FirstOrDefaultAsync(l => l.Email == req.Email && l.Phone == req.Phone, ct);
                if (lead is null)
                {
                    lead = new Lead
                    {
                        FullName = req.FullName,
                        Email = req.Email,
                        Phone = req.Phone,
                        City = req.City,
                        Graduation = req.Graduation,
                        YearOfGraduation = req.YearOfGraduation,
                        Source = LeadSource.Webinar,
                        Status = LeadStatus.New,
                        CourseInterestId = webinar.CourseId
                    };
                    db.Leads.Add(lead);
                }

                var alreadyRegistered = await db.WebinarRegistrations
                    .AnyAsync(r => r.WebinarId == webinar.Id && r.LeadId == lead.Id, ct);
                if (alreadyRegistered)
                    return Results.Ok(ApiResponse<object>.Ok(new { webinarId = webinar.Id, leadId = lead.Id, duplicate = true }, "Already registered"));

                if (webinar.Capacity.HasValue && await db.WebinarRegistrations.CountAsync(r => r.WebinarId == id, ct) >= webinar.Capacity.Value)
                    return Results.BadRequest(ApiResponse<object>.Fail("This session is fully booked. Please choose another session."));

                db.WebinarRegistrations.Add(new WebinarRegistration
                {
                    WebinarId = webinar.Id,
                    LeadId = lead.Id,
                    RegisteredAtUtc = DateTime.UtcNow
                });

                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                logger.LogInformation("Webinar registration: {WebinarId} <- {LeadEmail}", webinar.Id, req.Email);

                await dispatcher.SendAsync("webinar_confirmation", req.Email, new Dictionary<string, string?>
                {
                    ["full_name"] = req.FullName,
                    ["webinar_title"] = webinar.Title
                }, ct);

                return Results.Ok(ApiResponse<object>.Ok(
                    new { webinarId = webinar.Id, leadId = lead.Id, duplicate = false },
                    "Registration saved. Our team will share the joining details."));
            });
        }).WithName("RegisterForWebinar").RequireRateLimiting("public-post");

        // ---------- Contact enquiries ----------
        group.MapPost("/enquiries", async (
            CreateEnquiryRequest req,
            AppDbContext db,
            ILogger<Program> logger,
            ICaptchaVerifier captcha,
            HttpContext http,
            CancellationToken ct) =>
        {
            if (!await captcha.VerifyAsync(req.CaptchaToken, http.Connection.RemoteIpAddress?.ToString(), ct))
                return Results.BadRequest(ApiResponse<object>.Fail("Captcha verification failed. Please try again."));

            if (req.CourseInterestId.HasValue && !await db.Courses.AnyAsync(
                    c => c.Id == req.CourseInterestId.Value && c.IsPublished, ct))
                return Results.BadRequest(ApiResponse<object>.Fail("Please select an available program."));

            var enquiry = new ContactEnquiry
            {
                Name = req.Name.Trim(),
                Email = req.Email.Trim().ToLowerInvariant(),
                Phone = req.Phone,
                Subject = req.Subject,
                Message = req.Message.Trim(),
                SourcePage = req.SourcePage
            };
            db.ContactEnquiries.Add(enquiry);

            // Also create a lead so the counsellor team sees this
            var lead = new Lead
            {
                FullName = enquiry.Name,
                Email = enquiry.Email,
                Phone = req.Phone ?? string.Empty,
                Source = LeadSource.ContactForm,
                Status = LeadStatus.New,
                CourseInterestId = req.CourseInterestId,
                City = req.City?.Trim(),
                Graduation = req.Graduation?.Trim(),
                Notes = enquiry.Message
            };
            db.Leads.Add(lead);
            enquiry.ConvertedToLeadId = lead.Id;

            await db.SaveChangesAsync(ct);
            logger.LogInformation("Contact enquiry created from {Email}", req.Email);

            return Results.Ok(ApiResponse<object>.Ok(
                new { enquiryId = enquiry.Id, leadId = lead.Id },
                "Thank you! We will reach out within 24 hours."));
        }).WithName("CreateEnquiry").RequireRateLimiting("public-post");

        // ---------- Health ----------
        routes.MapGet("/ping", () => Results.Ok(new { status = "ok", utc = DateTime.UtcNow })).AllowAnonymous();

        return routes;
    }
}
