using NextGenCoaching.Core.Common;

namespace NextGenCoaching.Core.Entities;

public class EmailTemplate : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public string? BodyText { get; set; }
    public string? Description { get; set; }
}
