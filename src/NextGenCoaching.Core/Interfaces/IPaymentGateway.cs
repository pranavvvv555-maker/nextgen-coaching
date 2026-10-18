namespace NextGenCoaching.Core.Interfaces;

public interface IPaymentGateway
{
    Task<PaymentOrderResult> CreateOrderAsync(decimal amount, string currency, string receipt, CancellationToken ct = default);
    Task<bool> VerifyWebhookSignatureAsync(string payload, string signature);
}

public record PaymentOrderResult(string OrderId, decimal Amount, string Currency, string Status);
