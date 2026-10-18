using System.ComponentModel.DataAnnotations;

namespace NextGenCoaching.Shared.Requests;

/// <summary>
/// Used by both public (lead-driven) and authenticated student-driven checkout.
/// LeadId or StudentProfileId should be set, ideally both.
/// </summary>
public class CreateCheckoutRequest
{
    [Required]
    public Guid CourseId { get; set; }

    public Guid? BatchId { get; set; }

    public Guid? LeadId { get; set; }
}
