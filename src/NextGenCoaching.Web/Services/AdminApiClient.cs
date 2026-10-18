using NextGenCoaching.Shared.Constants;
using NextGenCoaching.Shared.Dtos;
using NextGenCoaching.Shared.Requests;
using NextGenCoaching.Shared.Responses;

namespace NextGenCoaching.Web.Services;

public class AdminApiClient
{
    private readonly ApiClient _api;
    public AdminApiClient(ApiClient api) => _api = api;

    // ---------- Dashboard ----------
    public Task<AdminDashboardStatsDto?> GetDashboardAsync(CancellationToken ct = default) =>
        _api.GetAsync<AdminDashboardStatsDto>($"{ApiRoutes.Admin.Reports}/dashboard", ct);

    // ---------- Leads ----------
    public Task<PagedResult<AdminLeadListItemDto>?> GetLeadsAsync(LeadListQuery q, CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(q.Status)) qs.Add($"status={Uri.EscapeDataString(q.Status)}");
        if (!string.IsNullOrWhiteSpace(q.Source)) qs.Add($"source={Uri.EscapeDataString(q.Source)}");
        if (!string.IsNullOrWhiteSpace(q.Search)) qs.Add($"search={Uri.EscapeDataString(q.Search)}");
        qs.Add($"page={q.Page}");
        qs.Add($"pageSize={q.PageSize}");
        var path = $"{ApiRoutes.Admin.Leads}/?{string.Join("&", qs)}";
        return _api.GetAsync<PagedResult<AdminLeadListItemDto>>(path, ct);
    }

    public Task<LeadDetailDto?> GetLeadAsync(Guid id, CancellationToken ct = default) =>
        _api.GetAsync<LeadDetailDto>($"{ApiRoutes.Admin.Leads}/{id}", ct);

    public Task<ApiResponse<object>?> UpdateLeadStatusAsync(Guid id, UpdateLeadStatusRequest req, CancellationToken ct = default) =>
        _api.PatchAsync<UpdateLeadStatusRequest, object>($"{ApiRoutes.Admin.Leads}/{id}/status", req, ct);

    public Task<ApiResponse<LeadActivityDto>?> LogLeadActivityAsync(Guid id, LogLeadActivityRequest req, CancellationToken ct = default) =>
        _api.PostAsync<LogLeadActivityRequest, LeadActivityDto>($"{ApiRoutes.Admin.Leads}/{id}/activities", req, ct);

    // ---------- Webinars ----------
    public Task<PagedResult<AdminWebinarDto>?> GetWebinarsAsync(int page = 1, int pageSize = 25, string? status = null, CancellationToken ct = default)
    {
        var qs = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(status)) qs.Add($"status={Uri.EscapeDataString(status)}");
        return _api.GetAsync<PagedResult<AdminWebinarDto>>($"{ApiRoutes.Admin.Webinars}/?{string.Join("&", qs)}", ct);
    }

    public Task<AdminWebinarDto?> GetWebinarAsync(Guid id, CancellationToken ct = default) =>
        _api.GetAsync<AdminWebinarDto>($"{ApiRoutes.Admin.Webinars}/{id}", ct);

    public Task<IReadOnlyList<AdminWebinarRegistrationDto>?> GetWebinarRegistrationsAsync(Guid id, CancellationToken ct = default) =>
        _api.GetAsync<IReadOnlyList<AdminWebinarRegistrationDto>>($"{ApiRoutes.Admin.Webinars}/{id}/registrations", ct);

    public Task<ApiResponse<Guid>?> CreateWebinarAsync(UpsertWebinarRequest req, CancellationToken ct = default) =>
        _api.PostAsync<UpsertWebinarRequest, Guid>($"{ApiRoutes.Admin.Webinars}/", req, ct);

    public Task<ApiResponse<Guid>?> UpdateWebinarAsync(Guid id, UpsertWebinarRequest req, CancellationToken ct = default) =>
        _api.PutAsync<UpsertWebinarRequest, Guid>($"{ApiRoutes.Admin.Webinars}/{id}", req, ct);

    public Task<ApiResponse<object>?> DeleteWebinarAsync(Guid id, CancellationToken ct = default) =>
        _api.DeleteAsync($"{ApiRoutes.Admin.Webinars}/{id}", ct);

    public Task<ApiResponse<object>?> ToggleAttendedAsync(Guid webinarId, Guid regId, CancellationToken ct = default) =>
        _api.PostAsync<object, object>($"{ApiRoutes.Admin.Webinars}/{webinarId}/registrations/{regId}/attended", new { }, ct);

    // ----- Courses -----
    public Task<IReadOnlyList<AdminCourseListItemDto>?> GetCoursesAsync(CancellationToken ct = default) =>
        _api.GetAsync<IReadOnlyList<AdminCourseListItemDto>>(ApiRoutes.Admin.Courses, ct);

    public Task<UpsertCourseRequest?> GetCourseAsync(Guid id, CancellationToken ct = default) =>
        _api.GetAsync<UpsertCourseRequest>($"{ApiRoutes.Admin.Courses}/{id}", ct);

    public Task<ApiResponse<Guid>?> CreateCourseAsync(UpsertCourseRequest req, CancellationToken ct = default) =>
        _api.PostAsync<UpsertCourseRequest, Guid>(ApiRoutes.Admin.Courses, req, ct);

    public Task<ApiResponse<Guid>?> UpdateCourseAsync(Guid id, UpsertCourseRequest req, CancellationToken ct = default) =>
        _api.PutAsync<UpsertCourseRequest, Guid>($"{ApiRoutes.Admin.Courses}/{id}", req, ct);

    public Task<ApiResponse<object>?> ToggleCoursePublishedAsync(Guid id, CancellationToken ct = default) =>
        _api.PostAsync<object, object>($"{ApiRoutes.Admin.Courses}/{id}/toggle-published", new { }, ct);

    public Task<IReadOnlyList<SelectOptionDto>?> GetCoursesLookupAsync(CancellationToken ct = default) =>
        _api.GetAsync<IReadOnlyList<SelectOptionDto>>($"{ApiRoutes.Admin.Courses}/lookup", ct);

    // ----- Batches -----
    public Task<IReadOnlyList<AdminBatchListItemDto>?> GetBatchesAsync(Guid? courseId = null, string? status = null, CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (courseId is not null)              qs.Add($"courseId={courseId.Value}");
        if (!string.IsNullOrWhiteSpace(status)) qs.Add($"status={Uri.EscapeDataString(status)}");
        var url = qs.Count == 0 ? ApiRoutes.Admin.Batches : $"{ApiRoutes.Admin.Batches}?{string.Join('&', qs)}";
        return _api.GetAsync<IReadOnlyList<AdminBatchListItemDto>>(url, ct);
    }

    public Task<UpsertBatchRequest?> GetBatchAsync(Guid id, CancellationToken ct = default) =>
        _api.GetAsync<UpsertBatchRequest>($"{ApiRoutes.Admin.Batches}/{id}", ct);

    public Task<ApiResponse<Guid>?> CreateBatchAsync(UpsertBatchRequest req, CancellationToken ct = default) =>
        _api.PostAsync<UpsertBatchRequest, Guid>(ApiRoutes.Admin.Batches, req, ct);

    public Task<ApiResponse<Guid>?> UpdateBatchAsync(Guid id, UpsertBatchRequest req, CancellationToken ct = default) =>
        _api.PutAsync<UpsertBatchRequest, Guid>($"{ApiRoutes.Admin.Batches}/{id}", req, ct);

    public Task<ApiResponse<object>?> UpdateBatchStatusAsync(Guid id, string status, CancellationToken ct = default) =>
        _api.PostAsync<object, object>($"{ApiRoutes.Admin.Batches}/{id}/status/{status}", new { }, ct);

    // ----- Instructors lookup -----
    public Task<IReadOnlyList<SelectOptionDto>?> GetInstructorsLookupAsync(CancellationToken ct = default) =>
        _api.GetAsync<IReadOnlyList<SelectOptionDto>>($"{ApiRoutes.Admin.Instructors}/lookup", ct);

    // ----- Students -----
    public Task<PagedResult<AdminStudentListItemDto>?> GetStudentsAsync(StudentListQuery q, CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(q.Status)) qs.Add($"status={Uri.EscapeDataString(q.Status)}");
        if (!string.IsNullOrWhiteSpace(q.Search)) qs.Add($"search={Uri.EscapeDataString(q.Search)}");
        qs.Add($"page={q.Page}");
        qs.Add($"pageSize={q.PageSize}");
        return _api.GetAsync<PagedResult<AdminStudentListItemDto>>($"{ApiRoutes.Admin.Students}/?{string.Join("&", qs)}", ct);
    }

    public Task<AdminStudentDetailDto?> GetStudentAsync(Guid id, CancellationToken ct = default) =>
        _api.GetAsync<AdminStudentDetailDto>($"{ApiRoutes.Admin.Students}/{id}", ct);

    // ----- Placements -----
    public Task<IReadOnlyList<AdminPlacementListItemDto>?> GetPlacementsAsync(string? search = null, CancellationToken ct = default)
    {
        var url = string.IsNullOrWhiteSpace(search)
            ? $"{ApiRoutes.Admin.Placements}/"
            : $"{ApiRoutes.Admin.Placements}/?search={Uri.EscapeDataString(search)}";
        return _api.GetAsync<IReadOnlyList<AdminPlacementListItemDto>>(url, ct);
    }

    // ----- Settings: email / SMTP (DB-driven) -----
    public Task<EmailSettingsDto?> GetEmailSettingsAsync(CancellationToken ct = default) =>
        _api.GetAsync<EmailSettingsDto>($"{ApiRoutes.Admin.Settings}/email", ct);

    public Task<ApiResponse<string>?> UpdateEmailSettingsAsync(UpdateEmailSettingsRequest req, CancellationToken ct = default) =>
        _api.PutAsync<UpdateEmailSettingsRequest, string>($"{ApiRoutes.Admin.Settings}/email", req, ct);

    public Task<ApiResponse<string>?> SendTestEmailAsync(string to, CancellationToken ct = default) =>
        _api.PostAsync<object, string>($"{ApiRoutes.Admin.Settings}/email/test?to={Uri.EscapeDataString(to)}", new { }, ct);

    // ----- Instructors -----
    public Task<IReadOnlyList<AdminInstructorListItemDto>?> GetInstructorsAsync(CancellationToken ct = default) =>
        _api.GetAsync<IReadOnlyList<AdminInstructorListItemDto>>($"{ApiRoutes.Admin.Instructors}/", ct);

    public Task<AdminInstructorDetailDto?> GetInstructorAsync(Guid id, CancellationToken ct = default) =>
        _api.GetAsync<AdminInstructorDetailDto>($"{ApiRoutes.Admin.Instructors}/{id}", ct);

    public Task<ApiResponse<Guid>?> CreateInstructorAsync(CreateInstructorRequest req, CancellationToken ct = default) =>
        _api.PostAsync<CreateInstructorRequest, Guid>($"{ApiRoutes.Admin.Instructors}/", req, ct);

    public Task<ApiResponse<object>?> UpdateInstructorAsync(Guid id, UpdateInstructorRequest req, CancellationToken ct = default) =>
        _api.PutAsync<UpdateInstructorRequest, object>($"{ApiRoutes.Admin.Instructors}/{id}", req, ct);

    // ----- Curriculum -----
    public Task<CourseCurriculumDto?> GetCurriculumAsync(Guid courseId, CancellationToken ct = default) =>
        _api.GetAsync<CourseCurriculumDto>($"{ApiRoutes.Admin.Curriculum}/{courseId}", ct);

    public Task<ApiResponse<Guid>?> AddToolAsync(Guid courseId, UpsertCurriculumToolRequest req, CancellationToken ct = default) =>
        _api.PostAsync<UpsertCurriculumToolRequest, Guid>($"{ApiRoutes.Admin.Curriculum}/{courseId}/tools", req, ct);
    public Task<ApiResponse<object>?> UpdateToolAsync(Guid id, UpsertCurriculumToolRequest req, CancellationToken ct = default) =>
        _api.PutAsync<UpsertCurriculumToolRequest, object>($"{ApiRoutes.Admin.Curriculum}/tools/{id}", req, ct);
    public Task<ApiResponse<object>?> DeleteToolAsync(Guid id, CancellationToken ct = default) =>
        _api.DeleteAsync($"{ApiRoutes.Admin.Curriculum}/tools/{id}", ct);

    public Task<ApiResponse<Guid>?> AddJourneyStepAsync(Guid courseId, UpsertCurriculumJourneyStepRequest req, CancellationToken ct = default) =>
        _api.PostAsync<UpsertCurriculumJourneyStepRequest, Guid>($"{ApiRoutes.Admin.Curriculum}/{courseId}/journey-steps", req, ct);
    public Task<ApiResponse<object>?> UpdateJourneyStepAsync(Guid id, UpsertCurriculumJourneyStepRequest req, CancellationToken ct = default) =>
        _api.PutAsync<UpsertCurriculumJourneyStepRequest, object>($"{ApiRoutes.Admin.Curriculum}/journey-steps/{id}", req, ct);
    public Task<ApiResponse<object>?> DeleteJourneyStepAsync(Guid id, CancellationToken ct = default) =>
        _api.DeleteAsync($"{ApiRoutes.Admin.Curriculum}/journey-steps/{id}", ct);

    public Task<ApiResponse<Guid>?> AddFeatureAsync(Guid courseId, UpsertCurriculumFeatureRequest req, CancellationToken ct = default) =>
        _api.PostAsync<UpsertCurriculumFeatureRequest, Guid>($"{ApiRoutes.Admin.Curriculum}/{courseId}/features", req, ct);
    public Task<ApiResponse<object>?> UpdateFeatureAsync(Guid id, UpsertCurriculumFeatureRequest req, CancellationToken ct = default) =>
        _api.PutAsync<UpsertCurriculumFeatureRequest, object>($"{ApiRoutes.Admin.Curriculum}/features/{id}", req, ct);
    public Task<ApiResponse<object>?> DeleteFeatureAsync(Guid id, CancellationToken ct = default) =>
        _api.DeleteAsync($"{ApiRoutes.Admin.Curriculum}/features/{id}", ct);

    public Task<ApiResponse<Guid>?> AddAssignmentAsync(Guid courseId, UpsertCurriculumAssignmentRequest req, CancellationToken ct = default) =>
        _api.PostAsync<UpsertCurriculumAssignmentRequest, Guid>($"{ApiRoutes.Admin.Curriculum}/{courseId}/assignments", req, ct);
    public Task<ApiResponse<object>?> UpdateAssignmentAsync(Guid id, UpsertCurriculumAssignmentRequest req, CancellationToken ct = default) =>
        _api.PutAsync<UpsertCurriculumAssignmentRequest, object>($"{ApiRoutes.Admin.Curriculum}/assignments/{id}", req, ct);
    public Task<ApiResponse<object>?> DeleteAssignmentAsync(Guid id, CancellationToken ct = default) =>
        _api.DeleteAsync($"{ApiRoutes.Admin.Curriculum}/assignments/{id}", ct);

    public Task<ApiResponse<Guid>?> AddProjectAsync(Guid courseId, UpsertCurriculumProjectRequest req, CancellationToken ct = default) =>
        _api.PostAsync<UpsertCurriculumProjectRequest, Guid>($"{ApiRoutes.Admin.Curriculum}/{courseId}/projects", req, ct);
    public Task<ApiResponse<object>?> UpdateProjectAsync(Guid id, UpsertCurriculumProjectRequest req, CancellationToken ct = default) =>
        _api.PutAsync<UpsertCurriculumProjectRequest, object>($"{ApiRoutes.Admin.Curriculum}/projects/{id}", req, ct);
    public Task<ApiResponse<object>?> DeleteProjectAsync(Guid id, CancellationToken ct = default) =>
        _api.DeleteAsync($"{ApiRoutes.Admin.Curriculum}/projects/{id}", ct);

    // ----- Mock interviews -----
    public Task<IReadOnlyList<AdminMockInterviewListItemDto>?> GetMockInterviewsAsync(string? status = null, CancellationToken ct = default)
    {
        var url = string.IsNullOrWhiteSpace(status)
            ? $"{ApiRoutes.Admin.MockInterviews}/"
            : $"{ApiRoutes.Admin.MockInterviews}/?status={Uri.EscapeDataString(status)}";
        return _api.GetAsync<IReadOnlyList<AdminMockInterviewListItemDto>>(url, ct);
    }

    public Task<IReadOnlyList<SelectOptionDto>?> GetMockEnrollmentsAsync(CancellationToken ct = default) =>
        _api.GetAsync<IReadOnlyList<SelectOptionDto>>($"{ApiRoutes.Admin.MockInterviews}/enrollments", ct);

    public Task<ApiResponse<Guid>?> ScheduleMockInterviewAsync(ScheduleMockInterviewRequest req, CancellationToken ct = default) =>
        _api.PostAsync<ScheduleMockInterviewRequest, Guid>($"{ApiRoutes.Admin.MockInterviews}/", req, ct);

    public Task<ApiResponse<object>?> RecordMockFeedbackAsync(Guid id, RecordMockInterviewFeedbackRequest req, CancellationToken ct = default) =>
        _api.PutAsync<RecordMockInterviewFeedbackRequest, object>($"{ApiRoutes.Admin.MockInterviews}/{id}/feedback", req, ct);

    // ----- Reports -----
    public Task<AdminReportDto?> GetReportAsync(CancellationToken ct = default) =>
        _api.GetAsync<AdminReportDto>($"{ApiRoutes.Admin.Reports}/", ct);

    // ----- Global search -----
    public Task<SearchResultsDto?> SearchAsync(string q, CancellationToken ct = default) =>
        _api.GetAsync<SearchResultsDto>($"{ApiRoutes.Admin.Search}/?q={Uri.EscapeDataString(q)}", ct);

    // ----- User management -----
    public Task<PagedResult<AdminUserListItemDto>?> GetUsersAsync(UserListQuery q, CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(q.Role)) qs.Add($"role={Uri.EscapeDataString(q.Role)}");
        if (!string.IsNullOrWhiteSpace(q.Search)) qs.Add($"search={Uri.EscapeDataString(q.Search)}");
        if (q.ActiveOnly == true) qs.Add("activeOnly=true");
        qs.Add($"page={q.Page}");
        qs.Add($"pageSize={q.PageSize}");
        return _api.GetAsync<PagedResult<AdminUserListItemDto>>($"{ApiRoutes.Admin.Users}/?{string.Join("&", qs)}", ct);
    }

    public Task<ApiResponse<object>?> ToggleUserActiveAsync(Guid id, CancellationToken ct = default) =>
        _api.PostAsync<object, object>($"{ApiRoutes.Admin.Users}/{id}/toggle-active", new { }, ct);

    public Task<ApiResponse<object>?> ResetUserPasswordAsync(Guid id, SetUserPasswordRequest req, CancellationToken ct = default) =>
        _api.PostAsync<SetUserPasswordRequest, object>($"{ApiRoutes.Admin.Users}/{id}/reset-password", req, ct);

    public Task<ApiResponse<object>?> SetUserRolesAsync(Guid id, SetUserRolesRequest req, CancellationToken ct = default) =>
        _api.PostAsync<SetUserRolesRequest, object>($"{ApiRoutes.Admin.Users}/{id}/roles", req, ct);
}
