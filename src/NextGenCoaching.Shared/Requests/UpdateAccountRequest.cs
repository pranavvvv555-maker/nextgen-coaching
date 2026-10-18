namespace NextGenCoaching.Shared.Requests;

public class UpdateAccountRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? City { get; set; }
}
