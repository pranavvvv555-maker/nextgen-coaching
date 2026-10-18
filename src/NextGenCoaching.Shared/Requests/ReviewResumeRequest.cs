using System.ComponentModel.DataAnnotations;

namespace NextGenCoaching.Shared.Requests;

public class ReviewResumeRequest
{
    /// <summary>Either "Approved" (good to share with employers) or "Reviewed" (revisions needed).</summary>
    [Required]
    public string Status { get; set; } = "Reviewed";

    [StringLength(3000)]
    public string? Notes { get; set; }
}
