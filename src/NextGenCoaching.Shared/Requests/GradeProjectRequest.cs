using System.ComponentModel.DataAnnotations;

namespace NextGenCoaching.Shared.Requests;

public class GradeProjectRequest
{
    [Required, Range(0, 100)]
    public decimal TotalScore { get; set; }

    [StringLength(3000)]
    public string? Feedback { get; set; }

    /// <summary>
    /// Optional JSON containing per-rubric-section scores. Stored verbatim;
    /// the rubric is defined per Project.RubricJson and the structure is
    /// instructor-defined for now.
    /// </summary>
    [StringLength(5000)]
    public string? EvaluationJson { get; set; }
}
