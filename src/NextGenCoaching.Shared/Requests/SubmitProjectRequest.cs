using System.ComponentModel.DataAnnotations;

namespace NextGenCoaching.Shared.Requests;

public class SubmitProjectRequest
{
    [Url, StringLength(500)]
    public string? RepoUrl { get; set; }

    [Url, StringLength(500)]
    public string? DeployedUrl { get; set; }

    [Url, StringLength(500)]
    public string? PresentationUrl { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }
}
