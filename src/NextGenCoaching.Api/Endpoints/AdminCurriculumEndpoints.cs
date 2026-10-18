using Microsoft.EntityFrameworkCore;
using NextGenCoaching.Core.Entities;
using NextGenCoaching.Core.Persistence;
using NextGenCoaching.Shared.Constants;
using NextGenCoaching.Shared.Dtos;
using NextGenCoaching.Shared.Requests;
using NextGenCoaching.Shared.Responses;

namespace NextGenCoaching.Api.Endpoints;

/// <summary>
/// CRUD for a course's curriculum sub-entities: tools, journey steps, features, assignments,
/// and projects. Reads come back as one aggregate; writes are per sub-entity.
/// </summary>
public static class AdminCurriculumEndpoints
{
    public static IEndpointRouteBuilder MapAdminCurriculumEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(ApiRoutes.Admin.Curriculum)
            .WithTags("Admin/Curriculum")
            .RequireAuthorization("RequireAdmin");

        // ---------- Aggregate read ----------
        group.MapGet("/{courseId:guid}", async (Guid courseId, AppDbContext db, CancellationToken ct) =>
        {
            var course = await db.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId, ct);
            if (course is null) return Results.NotFound(ApiResponse<CourseCurriculumDto>.Fail("Course not found"));

            var dto = new CourseCurriculumDto
            {
                CourseId = courseId,
                CourseTitle = course.Title,
                Tools = await db.CourseTools.AsNoTracking().Where(x => x.CourseId == courseId)
                    .OrderBy(x => x.SortOrder)
                    .Select(x => new CurriculumToolDto { Id = x.Id, ToolName = x.ToolName, IconUrl = x.IconUrl, Description = x.Description, SortOrder = x.SortOrder })
                    .ToListAsync(ct),
                JourneySteps = await db.CourseJourneySteps.AsNoTracking().Where(x => x.CourseId == courseId)
                    .OrderBy(x => x.StepNumber)
                    .Select(x => new CurriculumJourneyStepDto { Id = x.Id, StepNumber = x.StepNumber, Title = x.Title, Description = x.Description, Icon = x.Icon })
                    .ToListAsync(ct),
                Features = await db.CourseFeatures.AsNoTracking().Where(x => x.CourseId == courseId)
                    .OrderBy(x => x.SortOrder)
                    .Select(x => new CurriculumFeatureDto { Id = x.Id, Label = x.Label, Icon = x.Icon, SortOrder = x.SortOrder })
                    .ToListAsync(ct),
                Assignments = await db.Assignments.AsNoTracking().Where(x => x.CourseId == courseId)
                    .OrderBy(x => x.AssignmentNumber)
                    .Select(x => new CurriculumAssignmentDto { Id = x.Id, AssignmentNumber = x.AssignmentNumber, Title = x.Title, Description = x.Description, ResourcesUrl = x.ResourcesUrl, DueOffsetDays = x.DueOffsetDays, MaxScore = x.MaxScore, IsPublished = x.IsPublished })
                    .ToListAsync(ct),
                Projects = await db.Projects.AsNoTracking().Where(x => x.CourseId == courseId)
                    .OrderBy(x => x.ProjectNumber)
                    .Select(x => new CurriculumProjectDto { Id = x.Id, ProjectNumber = x.ProjectNumber, Title = x.Title, Brief = x.Brief, DatasetUrl = x.DatasetUrl, RecommendedDurationWeeks = x.RecommendedDurationWeeks })
                    .ToListAsync(ct)
            };
            return Results.Ok(ApiResponse<CourseCurriculumDto>.Ok(dto));
        }).WithName("AdminGetCurriculum");

        // ---------- Tools ----------
        group.MapPost("/{courseId:guid}/tools", async (Guid courseId, UpsertCurriculumToolRequest req, AppDbContext db, CancellationToken ct) =>
        {
            var e = new CourseTool { CourseId = courseId, ToolName = req.ToolName, IconUrl = req.IconUrl, Description = req.Description, SortOrder = req.SortOrder };
            db.CourseTools.Add(e);
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<Guid>.Ok(e.Id, "Tool added."));
        }).WithName("AdminCreateTool");

        group.MapPut("/tools/{id:guid}", async (Guid id, UpsertCurriculumToolRequest req, AppDbContext db, CancellationToken ct) =>
        {
            var e = await db.CourseTools.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (e is null) return Results.NotFound(ApiResponse<object>.Fail("Tool not found"));
            e.ToolName = req.ToolName; e.IconUrl = req.IconUrl; e.Description = req.Description; e.SortOrder = req.SortOrder;
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<object>.Ok(new { e.Id }, "Tool updated."));
        }).WithName("AdminUpdateTool");

        group.MapDelete("/tools/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var e = await db.CourseTools.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (e is null) return Results.NotFound(ApiResponse<object>.Fail("Tool not found"));
            db.CourseTools.Remove(e);   // soft-delete via the audit interceptor
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<object>.Ok(new { id }, "Tool removed."));
        }).WithName("AdminDeleteTool");

        // ---------- Journey steps ----------
        group.MapPost("/{courseId:guid}/journey-steps", async (Guid courseId, UpsertCurriculumJourneyStepRequest req, AppDbContext db, CancellationToken ct) =>
        {
            var e = new CourseJourneyStep { CourseId = courseId, StepNumber = req.StepNumber, Title = req.Title, Description = req.Description, Icon = req.Icon };
            db.CourseJourneySteps.Add(e);
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<Guid>.Ok(e.Id, "Step added."));
        }).WithName("AdminCreateJourneyStep");

        group.MapPut("/journey-steps/{id:guid}", async (Guid id, UpsertCurriculumJourneyStepRequest req, AppDbContext db, CancellationToken ct) =>
        {
            var e = await db.CourseJourneySteps.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (e is null) return Results.NotFound(ApiResponse<object>.Fail("Step not found"));
            e.StepNumber = req.StepNumber; e.Title = req.Title; e.Description = req.Description; e.Icon = req.Icon;
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<object>.Ok(new { e.Id }, "Step updated."));
        }).WithName("AdminUpdateJourneyStep");

        group.MapDelete("/journey-steps/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var e = await db.CourseJourneySteps.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (e is null) return Results.NotFound(ApiResponse<object>.Fail("Step not found"));
            db.CourseJourneySteps.Remove(e);
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<object>.Ok(new { id }, "Step removed."));
        }).WithName("AdminDeleteJourneyStep");

        // ---------- Features ----------
        group.MapPost("/{courseId:guid}/features", async (Guid courseId, UpsertCurriculumFeatureRequest req, AppDbContext db, CancellationToken ct) =>
        {
            var e = new CourseFeature { CourseId = courseId, Label = req.Label, Icon = req.Icon, SortOrder = req.SortOrder };
            db.CourseFeatures.Add(e);
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<Guid>.Ok(e.Id, "Feature added."));
        }).WithName("AdminCreateFeature");

        group.MapPut("/features/{id:guid}", async (Guid id, UpsertCurriculumFeatureRequest req, AppDbContext db, CancellationToken ct) =>
        {
            var e = await db.CourseFeatures.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (e is null) return Results.NotFound(ApiResponse<object>.Fail("Feature not found"));
            e.Label = req.Label; e.Icon = req.Icon; e.SortOrder = req.SortOrder;
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<object>.Ok(new { e.Id }, "Feature updated."));
        }).WithName("AdminUpdateFeature");

        group.MapDelete("/features/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var e = await db.CourseFeatures.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (e is null) return Results.NotFound(ApiResponse<object>.Fail("Feature not found"));
            db.CourseFeatures.Remove(e);
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<object>.Ok(new { id }, "Feature removed."));
        }).WithName("AdminDeleteFeature");

        // ---------- Assignments ----------
        group.MapPost("/{courseId:guid}/assignments", async (Guid courseId, UpsertCurriculumAssignmentRequest req, AppDbContext db, CancellationToken ct) =>
        {
            var e = new Assignment { CourseId = courseId, AssignmentNumber = req.AssignmentNumber, Title = req.Title, Description = req.Description, ResourcesUrl = req.ResourcesUrl, DueOffsetDays = req.DueOffsetDays, MaxScore = req.MaxScore, IsPublished = req.IsPublished };
            db.Assignments.Add(e);
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<Guid>.Ok(e.Id, "Assignment added."));
        }).WithName("AdminCreateAssignment");

        group.MapPut("/assignments/{id:guid}", async (Guid id, UpsertCurriculumAssignmentRequest req, AppDbContext db, CancellationToken ct) =>
        {
            var e = await db.Assignments.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (e is null) return Results.NotFound(ApiResponse<object>.Fail("Assignment not found"));
            e.AssignmentNumber = req.AssignmentNumber; e.Title = req.Title; e.Description = req.Description; e.ResourcesUrl = req.ResourcesUrl; e.DueOffsetDays = req.DueOffsetDays; e.MaxScore = req.MaxScore; e.IsPublished = req.IsPublished;
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<object>.Ok(new { e.Id }, "Assignment updated."));
        }).WithName("AdminUpdateAssignment");

        group.MapDelete("/assignments/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var e = await db.Assignments.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (e is null) return Results.NotFound(ApiResponse<object>.Fail("Assignment not found"));
            db.Assignments.Remove(e);
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<object>.Ok(new { id }, "Assignment removed."));
        }).WithName("AdminDeleteAssignment");

        // ---------- Projects ----------
        group.MapPost("/{courseId:guid}/projects", async (Guid courseId, UpsertCurriculumProjectRequest req, AppDbContext db, CancellationToken ct) =>
        {
            var e = new Project { CourseId = courseId, ProjectNumber = req.ProjectNumber, Title = req.Title, Brief = req.Brief, DatasetUrl = req.DatasetUrl, RecommendedDurationWeeks = req.RecommendedDurationWeeks };
            db.Projects.Add(e);
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<Guid>.Ok(e.Id, "Project added."));
        }).WithName("AdminCreateProject");

        group.MapPut("/projects/{id:guid}", async (Guid id, UpsertCurriculumProjectRequest req, AppDbContext db, CancellationToken ct) =>
        {
            var e = await db.Projects.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (e is null) return Results.NotFound(ApiResponse<object>.Fail("Project not found"));
            e.ProjectNumber = req.ProjectNumber; e.Title = req.Title; e.Brief = req.Brief; e.DatasetUrl = req.DatasetUrl; e.RecommendedDurationWeeks = req.RecommendedDurationWeeks;
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<object>.Ok(new { e.Id }, "Project updated."));
        }).WithName("AdminUpdateProject");

        group.MapDelete("/projects/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var e = await db.Projects.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (e is null) return Results.NotFound(ApiResponse<object>.Fail("Project not found"));
            db.Projects.Remove(e);
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<object>.Ok(new { id }, "Project removed."));
        }).WithName("AdminDeleteProject");

        return routes;
    }
}
