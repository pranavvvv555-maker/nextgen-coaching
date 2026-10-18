using NextGenCoaching.Core.Common;
using NextGenCoaching.Core.Enums;

namespace NextGenCoaching.Core.Entities;

public class PaymentTransaction : BaseEntity
{
    public string Provider { get; set; } = "razorpay";
    public string ProviderOrderId { get; set; } = string.Empty;
    public string? ProviderPaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public PaymentStatus Status { get; set; } = PaymentStatus.Created;

    public Guid? LeadId { get; set; }
    public Lead? Lead { get; set; }

    public Guid? StudentProfileId { get; set; }
    public StudentProfile? StudentProfile { get; set; }

    public Guid? CourseId { get; set; }
    public Course? Course { get; set; }

    public Guid? BatchId { get; set; }
    public Batch? Batch { get; set; }

    public string? RawResponseJson { get; set; }
    public DateTime? WebhookReceivedAtUtc { get; set; }
    public string? RefundReason { get; set; }
}
