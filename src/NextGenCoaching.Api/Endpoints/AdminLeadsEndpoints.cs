using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using NextGenCoaching.Core.Interfaces;
using NextGenCoaching.Core.Persistence;
using NextGenCoaching.Shared.Constants;
using NextGenCoaching.Shared.Dtos;
using NextGenCoaching.Shared.Enums;
using NextGenCoaching.Shared.Requests;
using NextGenCoaching.Shared.Responses;

namespace NextGenCoaching.Api.Endpoints;

public static class AdminLeadsEndpoints
{
    public static IEndpointRouteBuilder MapAdminLeadsEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(ApiRoutes.Admin.Leads)
            .WithTags("Admin · Leads")
            .RequireAuthorization("RequireCounsellor");

        // GET paged list
        group.MapGet("/", async (
            AppDbContext db,
            CancellationToken ct,
            int Page = 1, int PageSize = 25, string? Status = null, string? Source = null, string? Search = null) =>
        {
            var q = new LeadListQuery { Page = Page, PageSize = PageSize, Status = Status, Source = Source, Search = Search };
            var page = Math.Max(1, q.Page);
            var size = Math.Clamp(q.PageSize, 1, 100);

            var query = db.Leads.AsNoTracking()
                .Include(l => l.CourseInterest)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q.Status) && Enum.TryParse<LeadStatus>(q.Status, true, out var st))
                query = query.Where(l => l.Status == st);

            if (!string.IsNullOrWhiteSpace(q.Source) && Enum.TryParse<LeadSource>(q.Source, true, out var sr))
                query = query.Where(l => l.Source == sr);

            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                var s = q.Search.Trim();
                query = query.Where(l =>
                    EF.Functions.ILike(l.FullName, $"%{s}%") ||
                    EF.Functions.ILike(l.Email, $"%{s}%") ||
                    EF.Functions.ILike(l.Phone, $"%{s}%"));
            }

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(l => l.CreatedAtUtc)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(l => new AdminLeadListItemDto
                {
                    Id = l.Id,
                    FullName = l.FullName,
                    Email = l.Email,
                    Phone = l.Phone,
                    City = l.City,
                    Source = l.Source.ToString(),
                    Status = l.Status.ToString(),
                    CourseInterestTitle = l.CourseInterest != null ? l.CourseInterest.Title : null,
                    AssignedCounsellorName = null, // joined in a later phase when CounsellorProfile has a name lookup
                    NextFollowupAtUtc = l.NextFollowupAtUtc,
                    CreatedAtUtc = l.CreatedAtUtc
                })
                .ToListAsync(ct);

            return Results.Ok(ApiResponse<PagedResult<AdminLeadListItemDto>>.Ok(new PagedResult<AdminLeadListItemDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = size
            }));
        }).WithName("AdminListLeads");

        // GET single
        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var lead = await db.Leads.AsNoTracking()
                .Include(l => l.CourseInterest)
                .Include(l => l.Activities.OrderByDescending(a => a.OccurredAtUtc).Take(50))
                .FirstOrDefaultAsync(l => l.Id == id, ct);

            if (lead is null) return Results.NotFound(ApiResponse<LeadDetailDto>.Fail("Lead not found"));

            var dto = new LeadDetailDto
            {
                Id = lead.Id,
                FullName = lead.FullName,
                Email = lead.Email,
                Phone = lead.Phone,
                City = lead.City,
                Graduation = lead.Graduation,
                YearOfGraduation = lead.YearOfGraduation,
                Source = lead.Source.ToString(),
                SourceCampaign = lead.SourceCampaign,
                Status = lead.Status.ToString(),
                LostReason = lead.LostReason,
                CourseInterestTitle = lead.CourseInterest?.Title,
                AssignedCounsellorName = null, // joined when counsellor list endpoint lands
                Notes = lead.Notes,
                NextFollowupAtUtc = lead.NextFollowupAtUtc,
                CreatedAtUtc = lead.CreatedAtUtc,
                Activities = lead.Activities
                    .OrderByDescending(a => a.OccurredAtUtc)
                    .Select(a => new LeadActivityDto
                    {
                        Id = a.Id,
                        ActivityType = a.ActivityType.ToString(),
                        Outcome = a.Outcome,
                        Description = a.Description,
                        OccurredAtUtc = a.OccurredAtUtc
                    })
                    .ToList()
            };

            return Results.Ok(ApiResponse<LeadDetailDto>.Ok(dto));
        }).WithName("AdminGetLead");

        // PATCH status
        group.MapPatch("/{id:guid}/status", async (
            Guid id,
            UpdateLeadStatusRequest req,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<LeadStatus>(req.Status, true, out var newStatus))
                return Results.BadRequest(ApiResponse<object>.Fail($"Invalid status '{req.Status}'"));

            var lead = await db.Leads.FirstOrDefaultAsync(l => l.Id == id, ct);
            if (lead is null) return Results.NotFound(ApiResponse<object>.Fail("Lead not found"));

            var oldStatus = lead.Status;
            lead.Status = newStatus;
            lead.LostReason = newStatus == LeadStatus.Lost ? req.LostReason : null;

            db.LeadActivities.Add(new Core.Entities.LeadActivity
            {
                LeadId = lead.Id,
                ActivityType = Core.Enums.LeadActivityType.StatusChange,
                Outcome = newStatus.ToString(),
                Description = $"Status changed from {oldStatus} to {newStatus}",
                OccurredAtUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<object>.Ok(new { lead.Id, Status = lead.Status.ToString() }));
        }).WithName("AdminUpdateLeadStatus");

        // POST activity (log a call/note/email/etc.)
        group.MapPost("/{id:guid}/activities", async (
            Guid id,
            LogLeadActivityRequest req,
            AppDbContext db,
            ICurrentUserService user,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<Core.Enums.LeadActivityType>(req.ActivityType, true, out var type))
                return Results.BadRequest(ApiResponse<object>.Fail($"Invalid activity type '{req.ActivityType}'"));

            var lead = await db.Leads.FirstOrDefaultAsync(l => l.Id == id, ct);
            if (lead is null) return Results.NotFound(ApiResponse<object>.Fail("Lead not found"));

            var activity = new Core.Entities.LeadActivity
            {
                LeadId = lead.Id,
                ActivityType = type,
                Outcome = req.Outcome,
                Description = req.Description,
                OccurredAtUtc = req.OccurredAtUtc ?? DateTime.UtcNow,
                PerformedByUserId = user.UserId
            };
            db.LeadActivities.Add(activity);

            // Update next follow-up on the lead if supplied
            if (req.NextFollowupAtUtc.HasValue)
                lead.NextFollowupAtUtc = req.NextFollowupAtUtc;

            await db.SaveChangesAsync(ct);

            return Results.Ok(ApiResponse<LeadActivityDto>.Ok(new LeadActivityDto
            {
                Id = activity.Id,
                ActivityType = activity.ActivityType.ToString(),
                Outcome = activity.Outcome,
                Description = activity.Description,
                OccurredAtUtc = activity.OccurredAtUtc
            }));
        }).WithName("AdminLogLeadActivity");

        return routes;
    }
}
