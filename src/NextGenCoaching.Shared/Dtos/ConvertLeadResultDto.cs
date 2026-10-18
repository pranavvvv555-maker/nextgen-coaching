namespace NextGenCoaching.Shared.Dtos;

public class ConvertLeadResultDto
{
    public Guid? UserId { get; init; }
    public Guid? StudentProfileId { get; init; }

    /// <summary>The reset-password token. The Web layer turns this into a URL to email.</summary>
    public string? SetPasswordToken { get; init; }

    public string? SetPasswordUrl { get; init; }
    public bool AlreadyExisted { get; init; }
}
