using NextGenCoaching.Core.Common;

namespace NextGenCoaching.Core.Entities;

public class BlogTag : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<BlogPostTag> PostTags { get; set; } = new List<BlogPostTag>();
}
