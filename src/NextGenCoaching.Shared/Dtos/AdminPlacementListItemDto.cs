namespace NextGenCoaching.Shared.Dtos;

public class AdminPlacementListItemDto
{
    public Guid Id { get; init; }
    public Guid StudentProfileId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string RoleTitle { get; init; } = string.Empty;
    public string CourseTitle { get; init; } = string.Empty;
    public decimal? CtcLpa { get; init; }
    public DateOnly PlacementDate { get; init; }
    public bool IsFeatured { get; init; }
}
