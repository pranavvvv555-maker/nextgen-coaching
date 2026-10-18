using System.ComponentModel.DataAnnotations;

namespace NextGenCoaching.Shared.Requests;

public class UpdateLeadStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;

    [StringLength(200)]
    public string? LostReason { get; set; }
}
