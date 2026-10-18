using System.ComponentModel.DataAnnotations;

namespace NextGenCoaching.Shared.Requests;

public class RefundPaymentRequest
{
    /// <summary>If null, refunds the full amount.</summary>
    public decimal? Amount { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }
}
