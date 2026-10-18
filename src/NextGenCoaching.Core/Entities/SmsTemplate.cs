using NextGenCoaching.Core.Common;

namespace NextGenCoaching.Core.Entities;

public class SmsTemplate : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? DltTemplateId { get; set; }
}
