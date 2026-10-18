using NextGenCoaching.Core.Common;

namespace NextGenCoaching.Core.Entities;

public class ContactEnquiry : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Subject { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? SourcePage { get; set; }
    public Guid? ConvertedToLeadId { get; set; }
}
