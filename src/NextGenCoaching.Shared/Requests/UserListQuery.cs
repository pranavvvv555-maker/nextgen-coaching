namespace NextGenCoaching.Shared.Requests;

public class UserListQuery
{
    public string? Role { get; set; }
    public string? Search { get; set; }
    public bool? ActiveOnly { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
