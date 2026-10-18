namespace NextGenCoaching.Shared.Dtos;

public class PaymentSummaryDto
{
    public Guid Id { get; init; }
    public string Provider { get; init; } = "razorpay";
    public string ProviderOrderId { get; init; } = string.Empty;
    public string? ProviderPaymentId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "INR";
    public string Status { get; init; } = string.Empty;
    public string? CourseTitle { get; init; }
    public string? BatchName { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public bool InvoiceAvailable { get; init; }
}
