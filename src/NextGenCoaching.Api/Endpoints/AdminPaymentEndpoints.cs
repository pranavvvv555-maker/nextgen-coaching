using Microsoft.EntityFrameworkCore;
using NextGenCoaching.Core.Enums;
using NextGenCoaching.Core.Payments;
using NextGenCoaching.Core.Persistence;
using NextGenCoaching.Core.Services.Email;
using NextGenCoaching.Shared.Constants;
using NextGenCoaching.Shared.Dtos;
using NextGenCoaching.Shared.Requests;
using NextGenCoaching.Shared.Responses;

namespace NextGenCoaching.Api.Endpoints;

public static class AdminPaymentEndpoints
{
    public static IEndpointRouteBuilder MapAdminPaymentEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(ApiRoutes.Admin.Payments)
            .WithTags("Admin · Payments")
            .RequireAuthorization("RequireCounsellor");

        // POST /admin/leads/{id}/convert — kept here (not in AdminLeadsEndpoints) because it
        // depends on PaymentService and is tightly coupled to the enrolment flow.
        routes.MapPost(ApiRoutes.Admin.Leads + "/{id:guid}/convert", async (
            Guid id,
            PaymentService payments,
            HttpContext http,
            AppDbContext db,
            IEmailDispatcher dispatcher,
            CancellationToken ct) =>
        {
            var result = await payments.ConvertLeadToStudentAsync(id, ct);
            if (!result.Success)
                return Results.BadRequest(ApiResponse<ConvertLeadResultDto>.Fail(result.Error ?? "Conversion failed"));

            // Build the absolute set-password URL the admin can email to the student.
            // We point at the Web origin (Origin header on the admin request), falling back to "/set-password".
            string? setUrl = null;
            if (!string.IsNullOrEmpty(result.SetPasswordToken) && result.UserId.HasValue)
            {
                var origin = http.Request.Headers.Origin.ToString();
                var encodedToken = Uri.EscapeDataString(result.SetPasswordToken);
                setUrl = string.IsNullOrEmpty(origin)
                    ? $"/set-password?userId={result.UserId}&token={encodedToken}"
                    : $"{origin.TrimEnd('/')}/set-password?userId={result.UserId}&token={encodedToken}";
            }

            // Email the set-password link to the new student (fire-and-safe).
            if (setUrl is not null && result.UserId.HasValue)
            {
                var u = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == result.UserId.Value, ct);
                if (!string.IsNullOrEmpty(u?.Email))
                {
                    await dispatcher.SendAsync("set_password", u.Email, new Dictionary<string, string?>
                    {
                        ["full_name"] = u.FullName,
                        ["set_password_url"] = setUrl
                    }, ct);
                }
            }

            return Results.Ok(ApiResponse<ConvertLeadResultDto>.Ok(new ConvertLeadResultDto
            {
                UserId = result.UserId,
                StudentProfileId = result.StudentProfileId,
                SetPasswordToken = result.SetPasswordToken,
                SetPasswordUrl = setUrl,
                AlreadyExisted = result.AlreadyExistsMessage is not null
            }, result.AlreadyExistsMessage ?? "Lead converted"));
        })
        .WithTags("Admin · Leads")
        .RequireAuthorization("RequireCounsellor")
        .WithName("AdminConvertLead");

        // GET /admin/payments?page=&pageSize=&status=
        group.MapGet("/", async (
            AppDbContext db,
            int page = 1, int pageSize = 25, string? status = null,
            CancellationToken ct = default) =>
        {
            var p = Math.Max(1, page);
            var s = Math.Clamp(pageSize, 1, 100);

            var q = db.PaymentTransactions.AsNoTracking()
                .Include(t => t.Course)
                .Include(t => t.Batch)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PaymentStatus>(status, true, out var st))
                q = q.Where(t => t.Status == st);

            var total = await q.CountAsync(ct);
            var items = await q.OrderByDescending(t => t.CreatedAtUtc)
                .Skip((p - 1) * s).Take(s)
                .Select(t => new PaymentSummaryDto
                {
                    Id = t.Id,
                    Provider = t.Provider,
                    ProviderOrderId = t.ProviderOrderId,
                    ProviderPaymentId = t.ProviderPaymentId,
                    Amount = t.Amount,
                    Currency = t.Currency,
                    Status = t.Status.ToString(),
                    CourseTitle = t.Course != null ? t.Course.Title : null,
                    BatchName = t.Batch != null ? t.Batch.Name : null,
                    CreatedAtUtc = t.CreatedAtUtc,
                    InvoiceAvailable = t.Status == PaymentStatus.Captured || t.Status == PaymentStatus.Refunded
                })
                .ToListAsync(ct);

            return Results.Ok(ApiResponse<PagedResult<PaymentSummaryDto>>.Ok(new PagedResult<PaymentSummaryDto>
            {
                Items = items, TotalCount = total, Page = p, PageSize = s
            }));
        }).WithName("AdminListPayments");

        // POST /admin/payments/{id}/refund
        group.MapPost("/{id:guid}/refund", async (
            Guid id,
            RefundPaymentRequest req,
            PaymentService payments,
            CancellationToken ct) =>
        {
            var result = await payments.RefundAsync(id, req.Amount, req.Reason, ct);
            if (!result.Success)
                return Results.BadRequest(ApiResponse<object>.Fail(result.Error ?? "Refund failed"));

            return Results.Ok(ApiResponse<object>.Ok(new { refundId = result.RefundId, status = result.Status }));
        }).WithName("AdminRefundPayment");

        // GET /admin/payments/{id}/invoice — returns the PDF bytes
        group.MapGet("/{id:guid}/invoice", async (
            Guid id,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var tx = await db.PaymentTransactions.AsNoTracking()
                .Include(t => t.Course)
                .Include(t => t.Batch)
                .Include(t => t.StudentProfile).ThenInclude(p => p!.User)
                .Include(t => t.Lead)
                .FirstOrDefaultAsync(t => t.Id == id, ct);

            if (tx is null) return Results.NotFound();
            if (tx.Status != PaymentStatus.Captured && tx.Status != PaymentStatus.Refunded)
                return Results.BadRequest(ApiResponse<object>.Fail("Invoice only available for captured/refunded payments"));

            var data = new InvoiceData
            {
                InvoiceNumber = $"NG-{tx.CreatedAtUtc:yyMM}-{tx.Id.ToString()[..8].ToUpperInvariant()}",
                IssuedAt = tx.CreatedAtUtc,
                CustomerName = tx.StudentProfile?.User?.FullName ?? tx.Lead?.FullName ?? "Customer",
                CustomerEmail = tx.StudentProfile?.User?.Email ?? tx.Lead?.Email ?? string.Empty,
                CustomerPhone = tx.StudentProfile?.User?.PhoneNumber ?? tx.Lead?.Phone,
                CourseTitle = tx.Course?.Title ?? "NextGen course",
                BatchName = tx.Batch?.Name,
                Amount = tx.Amount,
                Currency = tx.Currency,
                Status = tx.Status.ToString(),
                ProviderOrderId = tx.ProviderOrderId,
                ProviderPaymentId = tx.ProviderPaymentId
            };
            var bytes = InvoiceGenerator.Generate(data);
            return Results.File(bytes, "application/pdf", $"{data.InvoiceNumber}.pdf");
        }).WithName("AdminDownloadInvoice");

        return routes;
    }
}
