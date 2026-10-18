using Microsoft.AspNetCore.Components;
using NextGenCoaching.Web.Services;

namespace NextGenCoaching.Web.Components;

public abstract class LocalizedComponentBase : ComponentBase
{
    [Inject] protected SitePreferences L { get; set; } = default!;
    [CascadingParameter(Name = "SitePreferencesRevision")] public int SitePreferencesRevision { get; set; }
}

public abstract class LocalizedLayoutBase : LayoutComponentBase
{
    [Inject] protected SitePreferences L { get; set; } = default!;
    [CascadingParameter(Name = "SitePreferencesRevision")] public int SitePreferencesRevision { get; set; }
}
