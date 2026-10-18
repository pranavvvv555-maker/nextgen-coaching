using NextGenCoaching.Shared.Constants;
using NextGenCoaching.Shared.Dtos;
using NextGenCoaching.Shared.Requests;
using NextGenCoaching.Shared.Responses;

namespace NextGenCoaching.Web.Services;

public class InstructorApiClient
{
    private readonly ApiClient _api;
    public InstructorApiClient(ApiClient api) => _api = api;

    public Task<InstructorDashboardDto?> GetDashboardAsync(CancellationToken ct = default) =>
        _api.GetAsync<InstructorDashboardDto>(ApiRoutes.Instructor.Dashboard, ct);

    public Task<IReadOnlyList<InstructorBatchSummaryDto>?> GetBatchesAsync(CancellationToken ct = default) =>
        _api.GetAsync<IReadOnlyList<InstructorBatchSummaryDto>>(ApiRoutes.Instructor.Batches, ct);

    // Assignments
    public Task<IReadOnlyList<GradingQueueItemDto>?> GetPendingAssignmentsAsync(CancellationToken ct = default) =>
        _api.GetAsync<IReadOnlyList<GradingQueueItemDto>>(ApiRoutes.Instructor.AssignmentsPending, ct);

    public Task<AssignmentGradingDto?> GetAssignmentSubmissionAsync(Guid id, CancellationToken ct = default) =>
        _api.GetAsync<AssignmentGradingDto>($"{ApiRoutes.Instructor.Submissions}/{id}", ct);

    public Task<ApiResponse<object>?> GradeAssignmentAsync(Guid id, GradeAssignmentRequest req, CancellationToken ct = default) =>
        _api.PostAsync<GradeAssignmentRequest, object>($"{ApiRoutes.Instructor.Submissions}/{id}/grade", req, ct);

    // Projects
    public Task<IReadOnlyList<GradingQueueItemDto>?> GetPendingProjectsAsync(CancellationToken ct = default) =>
        _api.GetAsync<IReadOnlyList<GradingQueueItemDto>>(ApiRoutes.Instructor.ProjectsPending, ct);

    public Task<ProjectGradingDto?> GetProjectSubmissionAsync(Guid id, CancellationToken ct = default) =>
        _api.GetAsync<ProjectGradingDto>($"{ApiRoutes.Instructor.ProjectSubmissions}/{id}", ct);

    public Task<ApiResponse<object>?> GradeProjectAsync(Guid id, GradeProjectRequest req, CancellationToken ct = default) =>
        _api.PostAsync<GradeProjectRequest, object>($"{ApiRoutes.Instructor.ProjectSubmissions}/{id}/grade", req, ct);

    // ---------- My profile ----------
    public Task<InstructorProfileDto?> GetProfileAsync(CancellationToken ct = default) =>
        _api.GetAsync<InstructorProfileDto>(ApiRoutes.Instructor.Profile, ct);

    public Task<ApiResponse<object>?> UpdateProfileAsync(UpdateInstructorRequest req, CancellationToken ct = default) =>
        _api.PutAsync<UpdateInstructorRequest, object>(ApiRoutes.Instructor.Profile, req, ct);

    // ---------- Attendance ----------
    public Task<IReadOnlyList<InstructorSessionListItemDto>?> GetAttendanceSessionsAsync(CancellationToken ct = default) =>
        _api.GetAsync<IReadOnlyList<InstructorSessionListItemDto>>($"{ApiRoutes.Instructor.Attendance}/sessions", ct);

    public Task<AttendanceRosterDto?> GetAttendanceRosterAsync(Guid sessionId, CancellationToken ct = default) =>
        _api.GetAsync<AttendanceRosterDto>($"{ApiRoutes.Instructor.Attendance}/sessions/{sessionId}", ct);

    public Task<ApiResponse<object>?> SaveAttendanceAsync(Guid sessionId, SaveAttendanceRequest req, CancellationToken ct = default) =>
        _api.PostAsync<SaveAttendanceRequest, object>($"{ApiRoutes.Instructor.Attendance}/sessions/{sessionId}", req, ct);
}
