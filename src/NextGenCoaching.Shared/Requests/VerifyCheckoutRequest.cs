using System.ComponentModel.DataAnnotations;

namespace NextGenCoaching.Shared.Requests;

public class VerifyCheckoutRequest
{
    [Required] public string RazorpayOrderId   { get; set; } = string.Empty;
    [Required] public string RazorpayPaymentId { get; set; } = string.Empty;
    [Required] public string RazorpaySignature { get; set; } = string.Empty;
}
