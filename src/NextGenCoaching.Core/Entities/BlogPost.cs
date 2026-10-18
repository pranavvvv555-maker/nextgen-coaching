using NextGenCoaching.Core.Common;

namespace NextGenCoaching.Core.Entities;

public class BlogPost : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string BodyMarkdown { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public Guid? AuthorUserId { get; set; }
    public int? ReadingMinutes { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public bool IsFeatured { get; set; }
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }

    public ICollection<BlogPostTag> PostTags { get; set; } = new List<BlogPostTag>();
}
