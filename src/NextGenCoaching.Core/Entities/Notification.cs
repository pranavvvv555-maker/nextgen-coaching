using NextGenCoaching.Core.Common;

namespace NextGenCoaching.Core.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? LinkUrl { get; set; }
    public string? PayloadJson { get; set; }
    public DateTime? ReadAtUtc { get; set; }
}
