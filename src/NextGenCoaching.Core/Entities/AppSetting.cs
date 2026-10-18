namespace NextGenCoaching.Core.Entities;

public class AppSetting
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Description { get; set; }
    public bool IsSecret { get; set; }
}
