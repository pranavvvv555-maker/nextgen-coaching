using NextGenCoaching.Core.Common;
using NextGenCoaching.Core.Identity;

namespace NextGenCoaching.Core.Entities;

public class InstructorProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public string? Headline { get; set; }
    public string? Bio { get; set; }
    public List<string> ExpertiseTags { get; set; } = new();
    public int? YearsExperience { get; set; }
    public string? LinkedinUrl { get; set; }
    public decimal? HourlyRate { get; set; }
    public bool IsPublic { get; set; } = true;

    public ICollection<Batch> Batches { get; set; } = new List<Batch>();
}
