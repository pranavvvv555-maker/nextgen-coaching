using System.ComponentModel.DataAnnotations;

namespace NextGenCoaching.Shared.Requests;

public class SubmitAssignmentRequest
{
    [Url, StringLength(500)]
    public string? SubmissionUrl { get; set; }

    [StringLength(5000)]
    public string? SubmissionText { get; set; }
}
