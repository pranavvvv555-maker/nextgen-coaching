namespace NextGenCoaching.Shared.Requests;

public class SetUserRolesRequest
{
    public List<string> Roles { get; set; } = new();
}
