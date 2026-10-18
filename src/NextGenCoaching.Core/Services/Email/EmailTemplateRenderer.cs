using Microsoft.EntityFrameworkCore;
using NextGenCoaching.Core.Persistence;

namespace NextGenCoaching.Core.Services.Email;

public sealed record RenderedEmail(string Subject, string Html, string? Text);

/// <summary>Renders an <c>EmailTemplate</c> row (looked up by key) with {{token}} substitution.</summary>
public interface ITemplateRenderer
{
    Task<RenderedEmail?> RenderAsync(string templateKey, IReadOnlyDictionary<string, string?> tokens, CancellationToken ct = default);
}

public sealed class EmailTemplateRenderer : ITemplateRenderer
{
    private static readonly System.Text.RegularExpressions.Regex TokenPattern =
        new(@"\{\{\s*(\w+)\s*\}\}", System.Text.RegularExpressions.RegexOptions.Compiled);

    private readonly AppDbContext _db;

    public EmailTemplateRenderer(AppDbContext db) => _db = db;

    public async Task<RenderedEmail?> RenderAsync(string templateKey, IReadOnlyDictionary<string, string?> tokens, CancellationToken ct = default)
    {
        var t = await _db.EmailTemplates.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Key == templateKey, ct);
        if (t is null) return null;

        return new RenderedEmail(
            Substitute(t.Subject, tokens),
            Substitute(t.BodyHtml, tokens),
            t.BodyText is null ? null : Substitute(t.BodyText, tokens));
    }

    // Unknown tokens are left as-is rather than blanked, which makes missing data obvious in testing.
    private static string Substitute(string template, IReadOnlyDictionary<string, string?> tokens)
    {
        if (string.IsNullOrEmpty(template)) return template;
        return TokenPattern.Replace(template, m =>
            tokens.TryGetValue(m.Groups[1].Value, out var v) ? (v ?? "") : m.Value);
    }
}
