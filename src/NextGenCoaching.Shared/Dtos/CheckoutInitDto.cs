namespace NextGenCoaching.Shared.Dtos;

public class CheckoutInitDto
{
    public Guid TransactionId { get; init; }
    public string RazorpayKeyId { get; init; } = string.Empty;
    public string ProviderOrderId { get; init; } = string.Empty;
    public long AmountInPaise { get; init; }
    public string Currency { get; init; } = "INR";
    public string CourseTitle { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public string CustomerPhone { get; init; } = string.Empty;
}
