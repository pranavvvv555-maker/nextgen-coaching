using NextGenCoaching.Shared.Constants;
using NextGenCoaching.Shared.Dtos;
using NextGenCoaching.Shared.Requests;
using NextGenCoaching.Shared.Responses;

namespace NextGenCoaching.Web.Services;

public class StudentApiClient
{
    private readonly ApiClient _api;
    public StudentApiClient(ApiClient api) => _api = api;

    // ---------- Dashboard ----------
    public Task<StudentDashboardDto?> GetDashboardAsync(CancellationToken ct = default) =>
        _api.GetAsync<StudentDashboardDto>(ApiRoutes.Student.Dashboard, ct);

    // ---------- Assignments ----------
    public Task<AssignmentDetailDto?> GetAssignmentAsync(Guid id, CancellationToken ct = default) =>
        _api.GetAsync<AssignmentDetailDto>($"{ApiRoutes.Student.Assignments}/{id}", ct);

    public Task<ApiResponse<object>?> SubmitAssignmentAsync(Guid id, SubmitAssignmentRequest req, CancellationToken ct = default) =>
        _api.PostAsync<SubmitAssignmentRequest, object>($"{ApiRoutes.Student.Assignments}/{id}/submit", req, ct);

    // ---------- Projects ----------
    public Task<IReadOnlyList<ProjectListItemDto>?> GetProjectsAsync(CancellationToken ct = default) =>
        _api.GetAsync<IReadOnlyList<ProjectListItemDto>>(ApiRoutes.Student.Projects, ct);

    public Task<ProjectDetailDto?> GetProjectAsync(Guid id, CancellationToken ct = default) =>
        _api.GetAsync<ProjectDetailDto>($"{ApiRoutes.Student.Projects}/{id}", ct);

    public Task<ApiResponse<object>?> SubmitProjectAsync(Guid id, SubmitProjectRequest req, CancellationToken ct = default) =>
        _api.PostAsync<SubmitProjectRequest, object>($"{ApiRoutes.Student.Projects}/{id}/submit", req, ct);

    // ---------- Profile ----------
    public Task<StudentProfileDto?> GetProfileAsync(CancellationToken ct = default) =>
        _api.GetAsync<StudentProfileDto>(ApiRoutes.Student.Profile, ct);

    public Task<ApiResponse<object>?> UpdateProfileAsync(UpdateProfileRequest req, CancellationToken ct = default) =>
        _api.PutAsync<UpdateProfileRequest, object>(ApiRoutes.Student.Profile, req, ct);

    // ---------- Notifications ----------
    public Task<IReadOnlyList<NotificationDto>?> GetNotificationsAsync(CancellationToken ct = default) =>
        _api.GetAsync<IReadOnlyList<NotificationDto>>(ApiRoutes.Student.Notifications, ct);

    public Task<ApiResponse<object>?> MarkNotificationReadAsync(Guid id, CancellationToken ct = default) =>
        _api.PostAsync<object, object>($"{ApiRoutes.Student.Notifications}/{id}/read", new { }, ct);

    public Task<ApiResponse<object>?> MarkAllReadAsync(CancellationToken ct = default) =>
        _api.PostAsync<object, object>($"{ApiRoutes.Student.Notifications}/read-all", new { }, ct);

    // ---------- Mock interviews ----------
    public Task<IReadOnlyList<StudentMockInterviewDetailDto>?> GetMockInterviewsAsync(CancellationToken ct = default) =>
        _api.GetAsync<IReadOnlyList<StudentMockInterviewDetailDto>>(ApiRoutes.Student.MockInterviews, ct);

    // ---------- Attendance ----------
    public Task<StudentAttendanceDto?> GetAttendanceAsync(CancellationToken ct = default) =>
        _api.GetAsync<StudentAttendanceDto>(ApiRoutes.Student.Attendance, ct);

    // ---------- Job board ----------
    public Task<IReadOnlyList<StudentJobDto>?> GetJobsAsync(CancellationToken ct = default) =>
        _api.GetAsync<IReadOnlyList<StudentJobDto>>(ApiRoutes.Student.Jobs, ct);

    public Task<ApiResponse<object>?> ApplyToJobAsync(Guid id, CancellationToken ct = default) =>
        _api.PostAsync<object, object>($"{ApiRoutes.Student.Jobs}/{id}/apply", new { }, ct);

    // ---------- Placements ----------
    public Task<IReadOnlyList<StudentPlacementDto>?> GetPlacementsAsync(CancellationToken ct = default) =>
        _api.GetAsync<IReadOnlyList<StudentPlacementDto>>(ApiRoutes.Student.Placements, ct);

    // ---------- Mock interview request ----------
    public Task<ApiResponse<object>?> RequestMockInterviewAsync(StudentMockInterviewRequest req, CancellationToken ct = default) =>
        _api.PostAsync<StudentMockInterviewRequest, object>($"{ApiRoutes.Student.MockInterviews}/request", req, ct);

    // ---------- Resume ----------
    public Task<IReadOnlyList<StudentResumeDto>?> GetResumesAsync(CancellationToken ct = default) =>
        _api.GetAsync<IReadOnlyList<StudentResumeDto>>(ApiRoutes.Student.Resumes, ct);

    public Task<ApiResponse<object>?> UploadResumeAsync(Stream content, string fileName, string contentType, CancellationToken ct = default) =>
        _api.UploadAsync<object>(ApiRoutes.Student.Resumes, content, fileName, contentType, "file", ct);
}
