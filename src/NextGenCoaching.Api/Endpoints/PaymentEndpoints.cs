using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NextGenCoaching.Core.Entities;
using NextGenCoaching.Core.Enums;
using NextGenCoaching.Core.Payments;
using NextGenCoaching.Core.Persistence;
using NextGenCoaching.Core.Services.Email;
using NextGenCoaching.Shared.Constants;
using NextGenCoaching.Shared.Dtos;
using NextGenCoaching.Shared.Requests;
using NextGenCoaching.Shared.Responses;

namespace NextGenCoaching.Api.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder routes)
    {
        // ---------- Public-facing checkout endpoints ----------
        var checkout = routes.MapGroup(ApiRoutes.Base + "/checkout")
            .WithTags("Checkout");

        checkout.MapPost("/init", async (
            CreateCheckoutRequest req,
            PaymentService payments,
            AppDbContext db,
            CancellationToken ct) =>
        {
            // Resolve studentProfileId if we have a lead with an associated user
            Guid? studentProfileId = null;
            string custName = string.Empty, custEmail = string.Empty, custPhone = string.Empty;

            if (req.LeadId is Guid leadId)
            {
                var lead = await db.Leads.AsNoTracking().FirstOrDefaultAsync(l => l.Id == leadId, ct);
                if (lead is not null)
                {
                    custName  = lead.FullName;
                    custEmail = lead.Email;
                    custPhone = lead.Phone;

                    var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == lead.Email, ct);
                    if (user is not null)
                    {
                        var profile = await db.StudentProfiles.AsNoTracking()
                            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
                        studentProfileId = profile?.Id;
                    }
                }
            }

            // Pick the next active batch for the course if none specified
            var batchId = req.BatchId;
            if (batchId is null)
            {
                var batch = await db.Batches.AsNoTracking()
                    .Where(b => b.CourseId == req.CourseId && b.Status == BatchStatus.Active)
                    .OrderBy(b => b.StartDate)
                    .FirstOrDefaultAsync(ct);
                batchId = batch?.Id;
            }

            var result = await payments.CreateCheckoutAsync(req.CourseId, req.LeadId, studentProfileId, batchId, ct);
            if (!result.Success)
                return Results.BadRequest(ApiResponse<CheckoutInitDto>.Fail(result.Error ?? "Could not create checkout"));

            return Results.Ok(ApiResponse<CheckoutInitDto>.Ok(new CheckoutInitDto
            {
                TransactionId    = result.TransactionId!.Value,
                RazorpayKeyId    = result.RazorpayKeyId!,
                ProviderOrderId  = result.ProviderOrderId!,
                AmountInPaise    = result.AmountInPaise,
                Currency         = result.Currency!,
                CourseTitle      = result.CourseTitle!,
                CustomerName     = custName,
                CustomerEmail    = custEmail,
                CustomerPhone    = custPhone
            }));
        }).WithName("CheckoutInit");

        checkout.MapPost("/verify", async (
            VerifyCheckoutRequest req,
            PaymentService payments,
            AppDbContext db,
            IEmailDispatcher dispatcher,
            CancellationToken ct) =>
        {
            var result = await payments.VerifyCheckoutAsync(req.RazorpayOrderId, req.RazorpayPaymentId, req.RazorpaySignature, ct);
            if (!result.Success)
                return Results.BadRequest(ApiResponse<object>.Fail(result.Error ?? "Verification failed"));

            // Email a payment receipt (fire-and-safe).
            if (result.TransactionId is Guid txId)
            {
                var tx = await db.PaymentTransactions.AsNoTracking()
                    .Include(t => t.Lead)
                    .Include(t => t.Course)
                    .FirstOrDefaultAsync(t => t.Id == txId, ct);

                if (!string.IsNullOrEmpty(tx?.Lead?.Email))
                {
                    await dispatcher.SendAsync("payment_receipt", tx.Lead.Email, new Dictionary<string, string?>
                    {
                        ["full_name"] = tx.Lead.FullName,
                        ["course_title"] = tx.Course?.Title ?? "your course",
                        ["amount"] = tx.Amount.ToString("0.##")
                    }, ct);
                }
            }

            return Results.Ok(ApiResponse<object>.Ok(new
            {
                transactionId = result.TransactionId,
                enrollmentId = result.EnrollmentId
            }));
        }).WithName("CheckoutVerify");

        // ---------- Webhook receiver (no auth — signature-validated) ----------
        routes.MapPost(ApiRoutes.Base + "/webhooks/razorpay", async (
            HttpContext ctx,
            PaymentService payments,
            AppDbContext db,
            ILogger<PaymentService> log) =>
        {
            string body;
            ctx.Request.EnableBuffering();
            using (var reader = new StreamReader(ctx.Request.Body, leaveOpen: true))
            {
                body = await reader.ReadToEndAsync();
            }
            ctx.Request.Body.Position = 0;

            var signature = ctx.Request.Headers["X-Razorpay-Signature"].ToString();
            if (string.IsNullOrEmpty(signature))
            {
                log.LogWarning("Webhook hit without X-Razorpay-Signature header");
                return Results.BadRequest(new { error = "Missing signature" });
            }

            var result = await payments.HandleWebhookAsync(body, signature);

            // Audit every signature-bearing webhook (accepted or rejected) for traceability.
            db.AuditLogs.Add(new AuditLog
            {
                Action = result.Accepted ? "RazorpayWebhookAccepted" : "RazorpayWebhookRejected",
                EntityName = "Webhook",
                NewValuesJson = System.Text.Json.JsonSerializer.Serialize(new { result.Accepted, result.Outcome, result.Error }),
                IpAddress = ctx.Connection.RemoteIpAddress?.ToString(),
                UserAgent = ctx.Request.Headers.UserAgent.ToString()
            });
            await db.SaveChangesAsync();

            if (!result.Accepted)
            {
                log.LogWarning("Webhook rejected: {Error}", result.Error);
                return Results.BadRequest(new { error = result.Error });
            }
            return Results.Ok(new { outcome = result.Outcome });
        })
        .WithTags("Webhooks · Razorpay")
        .WithName("RazorpayWebhook");

        return routes;
    }
}
