using NextGenCoaching.Core.Common;

namespace NextGenCoaching.Core.Entities;

public class CounsellorProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public int TargetLeadsPerDay { get; set; } = 20;
    public bool IsAvailable { get; set; } = true;
}
