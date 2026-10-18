using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NextGenCoaching.Core.Payments;

public class InvoiceDocument : IDocument
{
    public InvoiceData Data { get; }
    public InvoiceDocument(InvoiceData data) { Data = data; }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"NextGen Coaching · Invoice {Data.InvoiceNumber}",
        Author = "NextGen Graduate IT Coaching"
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(40);
            page.Size(PageSizes.A4);
            page.DefaultTextStyle(t => t.FontFamily("Helvetica").FontSize(10));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeBody);
            page.Footer().AlignCenter().Text(t =>
            {
                t.Span("NextGen Graduate IT Coaching · ").FontColor(Colors.Grey.Medium);
                t.Span("support@nextgencoaching.in · +91 7666834161").FontColor(Colors.Grey.Medium);
            });
        });
    }

    void ComposeHeader(IContainer c)
    {
        c.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("NextGen").FontSize(22).Bold().FontColor("#2563EB");
                col.Item().Text("Graduate IT Coaching").FontSize(10).FontColor(Colors.Grey.Darken1);
                col.Item().PaddingTop(4).Text("Bengaluru, India").FontSize(9).FontColor(Colors.Grey.Medium);
            });
            row.ConstantItem(200).AlignRight().Column(col =>
            {
                col.Item().Text("TAX INVOICE").FontSize(14).Bold();
                col.Item().Text(t =>
                {
                    t.Span("Invoice #: ").FontColor(Colors.Grey.Medium);
                    t.Span(Data.InvoiceNumber).Bold();
                });
                col.Item().Text(t =>
                {
                    t.Span("Issue date: ").FontColor(Colors.Grey.Medium);
                    t.Span(Data.IssuedAt.ToString("dd MMM yyyy"));
                });
                col.Item().Text(t =>
                {
                    t.Span("Status: ").FontColor(Colors.Grey.Medium);
                    t.Span(Data.Status).Bold().FontColor(Data.Status == "Captured" ? "#16A34A" : Colors.Grey.Darken1);
                });
            });
        });
    }

    void ComposeBody(IContainer c)
    {
        c.PaddingTop(20).Column(col =>
        {
            col.Spacing(15);

            // Billed to
            col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(b =>
            {
                b.Item().Text("BILL TO").FontSize(8).Bold().FontColor(Colors.Grey.Medium).LetterSpacing(0.05f);
                b.Item().PaddingTop(4).Text(Data.CustomerName).FontSize(11).Bold();
                b.Item().Text(Data.CustomerEmail).FontColor(Colors.Grey.Darken1);
                if (!string.IsNullOrEmpty(Data.CustomerPhone))
                    b.Item().Text(Data.CustomerPhone).FontColor(Colors.Grey.Darken1);
            });

            // Line item
            col.Item().Table(t =>
            {
                t.ColumnsDefinition(cd =>
                {
                    cd.RelativeColumn(5);
                    cd.RelativeColumn(2);
                });
                t.Header(h =>
                {
                    h.Cell().Background("#0F1B4C").Padding(8).Text("DESCRIPTION").FontColor(Colors.White).Bold().FontSize(9);
                    h.Cell().Background("#0F1B4C").Padding(8).AlignRight().Text("AMOUNT").FontColor(Colors.White).Bold().FontSize(9);
                });
                t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(col2 =>
                {
                    col2.Item().Text(Data.CourseTitle).Bold();
                    if (!string.IsNullOrEmpty(Data.BatchName))
                        col2.Item().PaddingTop(2).Text($"Batch: {Data.BatchName}").FontColor(Colors.Grey.Medium).FontSize(9);
                    col2.Item().PaddingTop(2).Text("Includes live sessions, projects, mock interviews, and placement assistance.").FontColor(Colors.Grey.Medium).FontSize(8);
                });
                t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(8).AlignRight().Text($"{Data.Currency} {Data.Amount:N2}").Bold();
            });

            // Totals
            col.Item().AlignRight().Column(totals =>
            {
                totals.Item().Text(t =>
                {
                    t.Span($"Subtotal: ").FontColor(Colors.Grey.Medium);
                    t.Span($"{Data.Currency} {Data.Amount:N2}");
                });
                totals.Item().PaddingTop(4).Text(t =>
                {
                    t.Span("TOTAL PAID: ").Bold();
                    t.Span($"{Data.Currency} {Data.Amount:N2}").Bold().FontSize(14).FontColor("#16A34A");
                });
            });

            // Payment ref
            col.Item().PaddingTop(20).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(p =>
            {
                p.Item().Text("PAYMENT DETAILS").FontSize(8).Bold().FontColor(Colors.Grey.Medium).LetterSpacing(0.05f);
                p.Item().PaddingTop(4).Text(t => { t.Span("Provider: ").FontColor(Colors.Grey.Medium); t.Span("Razorpay"); });
                p.Item().Text(t => { t.Span("Order ID: ").FontColor(Colors.Grey.Medium); t.Span(Data.ProviderOrderId); });
                if (!string.IsNullOrEmpty(Data.ProviderPaymentId))
                    p.Item().Text(t => { t.Span("Payment ID: ").FontColor(Colors.Grey.Medium); t.Span(Data.ProviderPaymentId); });
            });

            col.Item().PaddingTop(10).Text("This is a system-generated invoice. For queries, write to support@nextgencoaching.in.")
                .FontColor(Colors.Grey.Medium).FontSize(8).Italic();
        });
    }
}

public class InvoiceData
{
    public string InvoiceNumber { get; init; } = string.Empty;
    public DateTime IssuedAt { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public string? CustomerPhone { get; init; }
    public string CourseTitle { get; init; } = string.Empty;
    public string? BatchName { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "INR";
    public string Status { get; init; } = "Captured";
    public string ProviderOrderId { get; init; } = string.Empty;
    public string? ProviderPaymentId { get; init; }
}

public static class InvoiceGenerator
{
    public static byte[] Generate(InvoiceData data)
    {
        // QuestPDF v2024+ requires explicit licence acknowledgement
        QuestPDF.Settings.License = LicenseType.Community;
        return new InvoiceDocument(data).GeneratePdf();
    }
}
