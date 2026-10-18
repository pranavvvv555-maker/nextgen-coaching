using NextGenCoaching.Core.Common;

namespace NextGenCoaching.Core.Entities;

public class Faq : BaseEntity
{
    public string Category { get; set; } = "general";
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; } = true;
}
