using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NextGenCoaching.Core.Entities;
using NextGenCoaching.Core.Enums;
using NextGenCoaching.Core.Identity;
using NextGenCoaching.Core.Persistence;
using NextGenCoaching.Shared.Constants;
using SharedEnums = NextGenCoaching.Shared.Enums;

namespace NextGenCoaching.Core.Payments;

public class PaymentService
{
    private readonly AppDbContext _db;
    private readonly RazorpayClient _razorpay;
    private readonly UserManager<ApplicationUser> _userMgr;
    private readonly ILogger<PaymentService> _log;

    public PaymentService(
        AppDbContext db,
        RazorpayClient razorpay,
        UserManager<ApplicationUser> userMgr,
        ILogger<PaymentService> log)
    {
        _db = db;
        _razorpay = razorpay;
        _userMgr = userMgr;
        _log = log;
    }

    // ----------------------------------------------------------------
    // Step 1: Counsellor clicks "Convert" on a lead in the admin drawer.
    // We create the ApplicationUser + StudentProfile (no enrollment yet),
    // and return a one-time set-password token they email to the student.
    // ----------------------------------------------------------------
    public async Task<ConvertLeadResult> ConvertLeadToStudentAsync(Guid leadId, CancellationToken ct = default)
    {
        var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == leadId, ct);
        if (lead is null) return ConvertLeadResult.Fail("Lead not found");

        // Idempotency: if a user with this email already exists, link the lead and bail
        var existing = await _userMgr.FindByEmailAsync(lead.Email);
        if (existing is not null)
        {
            var existingProfile = await _db.StudentProfiles.FirstOrDefaultAsync(p => p.UserId == existing.Id, ct);
            return ConvertLeadResult.AlreadyExists(existing.Id, existingProfile?.Id);
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = lead.Email,
            Email = lead.Email,
            EmailConfirmed = false,                     // they confirm via the set-password link
            FullName = lead.FullName,
            PhoneNumber = lead.Phone,
            City = lead.City
        };
        // Random throwaway password — they'll set their own via the email link
        var tempPwd = Guid.NewGuid().ToString("N") + "!Aa1";
        var create = await _userMgr.CreateAsync(user, tempPwd);
        if (!create.Succeeded)
        {
            _log.LogError("ConvertLead: user create failed for {Email}: {Errors}",
                lead.Email, string.Join(", ", create.Errors.Select(e => e.Description)));
            return ConvertLeadResult.Fail("Could not create user: " + string.Join(", ", create.Errors.Select(e => e.Description)));
        }
        await _userMgr.AddToRoleAsync(user, RoleNames.Student);

        var profile = new StudentProfile
        {
            UserId = user.Id,
            Status = StudentStatus.Active
        };
        _db.StudentProfiles.Add(profile);

        // Move the lead into Enrolled-pending state
        lead.Status = SharedEnums.LeadStatus.Negotiation; // counsellor will flip to Enrolled after payment

        _db.LeadActivities.Add(new LeadActivity
        {
            LeadId = lead.Id,
            ActivityType = LeadActivityType.StatusChange,
            Outcome = "Converted",
            Description = $"Lead converted into student user {user.Email}. Awaiting payment.",
            OccurredAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        // Set-password token. The Web layer turns this into a clickable URL.
        var token = await _userMgr.GeneratePasswordResetTokenAsync(user);

        return ConvertLeadResult.Ok(user.Id, profile.Id, token);
    }

    // ----------------------------------------------------------------
    // Step 2: Student (or anyone with the lead link) initiates checkout for a course.
    // We create a Razorpay order and a local PaymentTransaction in Created state.
    // The page then renders the Razorpay JS modal using order.id + key id.
    // ----------------------------------------------------------------
    public async Task<CreateCheckoutResult> CreateCheckoutAsync(
        Guid courseId,
        Guid? leadId,
        Guid? studentProfileId,
        Guid? batchId,
        CancellationToken ct = default)
    {
        if (!_razorpay.IsConfigured)
            return CreateCheckoutResult.Fail("Payment provider is not configured. Set Razorpay:KeyId and KeySecret.");

        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == courseId && c.IsPublished, ct);
        if (course is null) return CreateCheckoutResult.Fail("Course not found or unpublished");

        var amount = course.DiscountPrice ?? course.Price;
        if (amount <= 0) return CreateCheckoutResult.Fail("Course has no price configured");

        // Razorpay wants paise (smallest unit) as a long
        var paise = (long)Math.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);

        var receipt = $"course-{course.Slug}-{DateTime.UtcNow:yyMMddHHmmss}";
        var notes = new Dictionary<string, string>
        {
            ["courseId"] = courseId.ToString(),
            ["courseSlug"] = course.Slug
        };
        if (leadId.HasValue)            notes["leadId"] = leadId.Value.ToString();
        if (studentProfileId.HasValue)  notes["studentProfileId"] = studentProfileId.Value.ToString();
        if (batchId.HasValue)           notes["batchId"] = batchId.Value.ToString();

        var order = await _razorpay.CreateOrderAsync(paise, receipt, notes, ct);
        if (order is null) return CreateCheckoutResult.Fail("Razorpay rejected the order. Check API logs.");

        var tx = new PaymentTransaction
        {
            Provider = "razorpay",
            ProviderOrderId = order.Id,
            Amount = amount,
            Currency = order.Currency,
            Status = PaymentStatus.Created,
            LeadId = leadId,
            StudentProfileId = studentProfileId,
            CourseId = courseId,
            BatchId = batchId
        };
        _db.PaymentTransactions.Add(tx);
        await _db.SaveChangesAsync(ct);

        return CreateCheckoutResult.Ok(
            transactionId: tx.Id,
            providerOrderId: order.Id,
            amountInPaise: paise,
            currency: order.Currency,
            razorpayKeyId: _razorpay.KeyId,
            courseTitle: course.Title);
    }

    // ----------------------------------------------------------------
    // Step 3: After the Razorpay JS modal completes, the browser posts
    // {orderId, paymentId, signature} back here. We verify the signature,
    // mark the transaction Captured, and create the Enrollment.
    // ----------------------------------------------------------------
    public async Task<VerifyCheckoutResult> VerifyCheckoutAsync(
        string orderId,
        string paymentId,
        string signature,
        CancellationToken ct = default)
    {
        if (!_razorpay.VerifyCheckoutSignature(orderId, paymentId, signature))
        {
            _log.LogWarning("Razorpay signature mismatch for order {OrderId} payment {PaymentId}", orderId, paymentId);
            return VerifyCheckoutResult.Fail("Signature mismatch — possible tampering");
        }

        var tx = await _db.PaymentTransactions
            .FirstOrDefaultAsync(t => t.ProviderOrderId == orderId, ct);
        if (tx is null) return VerifyCheckoutResult.Fail("No matching transaction for that order");

        // Already captured? Idempotent — just return the enrolment id we already have
        if (tx.Status == PaymentStatus.Captured)
        {
            var existing = await _db.Enrollments
                .FirstOrDefaultAsync(e => e.StudentProfileId == tx.StudentProfileId && e.BatchId == tx.BatchId, ct);
            return VerifyCheckoutResult.Ok(tx.Id, existing?.Id);
        }

        tx.ProviderPaymentId = paymentId;
        tx.Status = PaymentStatus.Captured;

        // Pull the payment object for the receipt JSON
        var payment = await _razorpay.GetPaymentAsync(paymentId, ct);
        if (payment is not null)
            tx.RawResponseJson = System.Text.Json.JsonSerializer.Serialize(payment);

        // Promote lead → Enrolled
        if (tx.LeadId.HasValue)
        {
            var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == tx.LeadId.Value, ct);
            if (lead is not null) lead.Status = SharedEnums.LeadStatus.Enrolled;
        }

        // Create the Enrollment (if we have student + batch)
        Guid? enrollmentId = null;
        if (tx.StudentProfileId.HasValue && tx.BatchId.HasValue)
        {
            var already = await _db.Enrollments.FirstOrDefaultAsync(
                e => e.StudentProfileId == tx.StudentProfileId.Value && e.BatchId == tx.BatchId.Value, ct);
            if (already is null)
            {
                var enr = new Enrollment
                {
                    StudentProfileId = tx.StudentProfileId.Value,
                    BatchId = tx.BatchId.Value,
                    EnrolledAtUtc = DateTime.UtcNow,
                    AmountPaid = tx.Amount,
                    Status = SharedEnums.EnrollmentStatus.Active,
                    CompletionPercentage = 0
                };
                _db.Enrollments.Add(enr);
                enrollmentId = enr.Id;
            }
            else
            {
                enrollmentId = already.Id;
            }
        }

        await _db.SaveChangesAsync(ct);
        return VerifyCheckoutResult.Ok(tx.Id, enrollmentId);
    }

    // ----------------------------------------------------------------
    // Step 4: Webhook receiver.
    // Razorpay calls /webhooks/razorpay with event payloads. We rely on
    // the signed body, NOT the JSON contents, for trust. Idempotency
    // comes from the (provider_order_id, status) key — we only transition
    // forward, never back.
    // ----------------------------------------------------------------
    public async Task<WebhookResult> HandleWebhookAsync(
        string rawBody, string signature, CancellationToken ct = default)
    {
        if (!_razorpay.VerifyWebhookSignature(rawBody, signature))
            return WebhookResult.Reject("Webhook signature invalid");

        using var doc = System.Text.Json.JsonDocument.Parse(rawBody);
        var root = doc.RootElement;
        if (!root.TryGetProperty("event", out var evtElem))
            return WebhookResult.Reject("Missing 'event' field");

        var evt = evtElem.GetString();
        _log.LogInformation("Razorpay webhook received: {Event}", evt);

        switch (evt)
        {
            case "payment.captured":
                return await HandlePaymentCapturedAsync(root, ct);
            case "payment.failed":
                return await HandlePaymentFailedAsync(root, ct);
            case "refund.processed":
                return await HandleRefundProcessedAsync(root, ct);
            default:
                _log.LogInformation("Ignoring unhandled Razorpay event {Event}", evt);
                return WebhookResult.Ignored(evt ?? "");
        }
    }

    private async Task<WebhookResult> HandlePaymentCapturedAsync(System.Text.Json.JsonElement root, CancellationToken ct)
    {
        var payment = root.GetProperty("payload").GetProperty("payment").GetProperty("entity");
        var orderId = payment.GetProperty("order_id").GetString() ?? string.Empty;
        var paymentId = payment.GetProperty("id").GetString() ?? string.Empty;

        var tx = await _db.PaymentTransactions.FirstOrDefaultAsync(t => t.ProviderOrderId == orderId, ct);
        if (tx is null) return WebhookResult.Reject($"No tx for order {orderId}");

        tx.WebhookReceivedAtUtc = DateTime.UtcNow;

        if (tx.Status == PaymentStatus.Captured) return WebhookResult.Ok("already-captured");

        // The browser may not have hit /verify yet (user closed tab). We still mark captured
        // and create the enrollment — webhook is the source of truth.
        tx.ProviderPaymentId = paymentId;
        tx.Status = PaymentStatus.Captured;

        if (tx.StudentProfileId.HasValue && tx.BatchId.HasValue)
        {
            var enr = await _db.Enrollments.FirstOrDefaultAsync(
                e => e.StudentProfileId == tx.StudentProfileId.Value && e.BatchId == tx.BatchId.Value, ct);
            if (enr is null)
            {
                _db.Enrollments.Add(new Enrollment
                {
                    StudentProfileId = tx.StudentProfileId.Value,
                    BatchId = tx.BatchId.Value,
                    EnrolledAtUtc = DateTime.UtcNow,
                    AmountPaid = tx.Amount,
                    Status = SharedEnums.EnrollmentStatus.Active
                });
            }
        }

        if (tx.LeadId.HasValue)
        {
            var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == tx.LeadId.Value, ct);
            if (lead is not null) lead.Status = SharedEnums.LeadStatus.Enrolled;
        }

        await _db.SaveChangesAsync(ct);
        return WebhookResult.Ok("captured");
    }

    private async Task<WebhookResult> HandlePaymentFailedAsync(System.Text.Json.JsonElement root, CancellationToken ct)
    {
        var payment = root.GetProperty("payload").GetProperty("payment").GetProperty("entity");
        var orderId = payment.GetProperty("order_id").GetString() ?? string.Empty;
        var tx = await _db.PaymentTransactions.FirstOrDefaultAsync(t => t.ProviderOrderId == orderId, ct);
        if (tx is null) return WebhookResult.Reject($"No tx for order {orderId}");

        tx.WebhookReceivedAtUtc = DateTime.UtcNow;
        if (tx.Status == PaymentStatus.Created || tx.Status == PaymentStatus.Authorized)
            tx.Status = PaymentStatus.Failed;
        await _db.SaveChangesAsync(ct);
        return WebhookResult.Ok("failed");
    }

    private async Task<WebhookResult> HandleRefundProcessedAsync(System.Text.Json.JsonElement root, CancellationToken ct)
    {
        var refund = root.GetProperty("payload").GetProperty("refund").GetProperty("entity");
        var paymentId = refund.GetProperty("payment_id").GetString() ?? string.Empty;
        var tx = await _db.PaymentTransactions.FirstOrDefaultAsync(t => t.ProviderPaymentId == paymentId, ct);
        if (tx is null) return WebhookResult.Reject($"No tx for payment {paymentId}");

        tx.Status = PaymentStatus.Refunded;
        tx.WebhookReceivedAtUtc = DateTime.UtcNow;
        // Best-effort: mark linked enrollment refunded too
        if (tx.StudentProfileId.HasValue && tx.BatchId.HasValue)
        {
            var enr = await _db.Enrollments.FirstOrDefaultAsync(
                e => e.StudentProfileId == tx.StudentProfileId.Value && e.BatchId == tx.BatchId.Value, ct);
            if (enr is not null) enr.Status = SharedEnums.EnrollmentStatus.Refunded;
        }
        await _db.SaveChangesAsync(ct);
        return WebhookResult.Ok("refunded");
    }

    // ----------------------------------------------------------------
    // Admin-initiated refund.
    // ----------------------------------------------------------------
    public async Task<RefundResult> RefundAsync(Guid transactionId, decimal? amount, string? reason, CancellationToken ct = default)
    {
        var tx = await _db.PaymentTransactions.FirstOrDefaultAsync(t => t.Id == transactionId, ct);
        if (tx is null) return RefundResult.Fail("Transaction not found");
        if (tx.Status != PaymentStatus.Captured) return RefundResult.Fail($"Cannot refund a transaction in status {tx.Status}");
        if (string.IsNullOrEmpty(tx.ProviderPaymentId)) return RefundResult.Fail("Transaction has no payment id");

        long? paise = amount.HasValue ? (long)Math.Round(amount.Value * 100m, 0, MidpointRounding.AwayFromZero) : null;
        var notes = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(reason)) notes["reason"] = reason!;

        var refund = await _razorpay.RefundPaymentAsync(tx.ProviderPaymentId, paise, notes, ct);
        if (refund is null) return RefundResult.Fail("Razorpay rejected the refund");

        tx.RefundReason = reason;
        // Don't set Status=Refunded until the webhook confirms (refund.processed).
        // Razorpay refunds can be async (hours to days), so we trust the webhook as source of truth.
        await _db.SaveChangesAsync(ct);

        return RefundResult.Ok(refund.Id, refund.Status);
    }
}

// ---------- Result records (tiny DTOs for the service layer) ----------

public record ConvertLeadResult(bool Success, Guid? UserId, Guid? StudentProfileId, string? SetPasswordToken, string? AlreadyExistsMessage, string? Error)
{
    public static ConvertLeadResult Ok(Guid userId, Guid profileId, string token) =>
        new(true, userId, profileId, token, null, null);
    public static ConvertLeadResult AlreadyExists(Guid userId, Guid? profileId) =>
        new(true, userId, profileId, null, "User with this email already exists; linked.", null);
    public static ConvertLeadResult Fail(string error) =>
        new(false, null, null, null, null, error);
}

public record CreateCheckoutResult(bool Success, Guid? TransactionId, string? ProviderOrderId, long AmountInPaise, string? Currency, string? RazorpayKeyId, string? CourseTitle, string? Error)
{
    public static CreateCheckoutResult Ok(Guid transactionId, string providerOrderId, long amountInPaise, string currency, string razorpayKeyId, string courseTitle) =>
        new(true, transactionId, providerOrderId, amountInPaise, currency, razorpayKeyId, courseTitle, null);
    public static CreateCheckoutResult Fail(string error) =>
        new(false, null, null, 0, null, null, null, error);
}

public record VerifyCheckoutResult(bool Success, Guid? TransactionId, Guid? EnrollmentId, string? Error)
{
    public static VerifyCheckoutResult Ok(Guid txId, Guid? enrId) => new(true, txId, enrId, null);
    public static VerifyCheckoutResult Fail(string error) => new(false, null, null, error);
}

public record WebhookResult(bool Accepted, string Outcome, string? Error)
{
    public static WebhookResult Ok(string outcome) => new(true, outcome, null);
    public static WebhookResult Ignored(string evt) => new(true, $"ignored-{evt}", null);
    public static WebhookResult Reject(string error) => new(false, "rejected", error);
}

public record RefundResult(bool Success, string? RefundId, string? Status, string? Error)
{
    public static RefundResult Ok(string refundId, string status) => new(true, refundId, status, null);
    public static RefundResult Fail(string error) => new(false, null, null, error);
}
