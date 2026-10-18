namespace NextGenCoaching.Shared.Requests;

public class StudentListQuery
{
    public string? Status { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
