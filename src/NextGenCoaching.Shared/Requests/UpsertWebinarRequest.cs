using System.ComponentModel.DataAnnotations;

namespace NextGenCoaching.Shared.Requests;

public class UpsertWebinarRequest
{
    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public Guid? CourseId { get; set; }

    [Required]
    public DateTime ScheduledAtUtc { get; set; }

    [Range(15, 240)]
    public int DurationMinutes { get; set; } = 60;

    [StringLength(500)]
    public string? JoinUrl { get; set; }

    [Range(1, 10000)]
    public int? Capacity { get; set; }

    [StringLength(500)]
    public string? BannerImageUrl { get; set; }
}
