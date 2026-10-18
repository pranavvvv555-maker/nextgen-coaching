using System.ComponentModel.DataAnnotations;

namespace NextGenCoaching.Shared.Requests;

public class GradeAssignmentRequest
{
    [Required, Range(0, 1000)]
    public decimal Score { get; set; }

    [StringLength(3000)]
    public string? Feedback { get; set; }
}
