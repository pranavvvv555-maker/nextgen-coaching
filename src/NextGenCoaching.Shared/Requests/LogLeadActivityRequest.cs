using System.ComponentModel.DataAnnotations;

namespace NextGenCoaching.Shared.Requests;

public class LogLeadActivityRequest
{
    [Required, StringLength(30)]
    public string ActivityType { get; set; } = "Note";   // Call | Email | Sms | WhatsApp | Note | Meeting

    [StringLength(50)]
    public string? Outcome { get; set; }

    [Required, StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    public DateTime? OccurredAtUtc { get; set; }

    public DateTime? NextFollowupAtUtc { get; set; }
}
