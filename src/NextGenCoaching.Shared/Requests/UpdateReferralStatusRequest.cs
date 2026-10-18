using System.ComponentModel.DataAnnotations;

namespace NextGenCoaching.Shared.Requests;

public class UpdateReferralStatusRequest
{
    [Required] public string Status { get; set; } = "Referred";
    public decimal? OfferedCtcLpa { get; set; }
    public DateOnly? JoiningDate { get; set; }
    [StringLength(1000)] public string? Notes { get; set; }
}
