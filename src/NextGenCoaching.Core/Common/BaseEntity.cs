namespace NextGenCoaching.Core.Common;

/// <summary>
/// Base for every persistent entity. Carries Id + audit + soft-delete columns.
/// </summary>
public abstract class BaseEntity : IAuditable, ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
}
