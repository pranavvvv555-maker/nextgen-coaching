using NextGenCoaching.Shared.Constants;
using NextGenCoaching.Shared.Dtos;

namespace NextGenCoaching.Web.Services;

public class CourseApiClient
{
    private readonly ApiClient _api;
    public CourseApiClient(ApiClient api) => _api = api;

    public async Task<IReadOnlyList<CourseSummaryDto>?> GetCoursesAsync(string? category = null, CancellationToken ct = default)
    {
        var path = ApiRoutes.Public.Courses + (string.IsNullOrWhiteSpace(category) ? string.Empty : $"?category={Uri.EscapeDataString(category)}");
        var res = await _api.GetAsync<IReadOnlyList<CourseSummaryDto>>(path, ct);
        if (res is { Count: > 0 }) return res;

        // Fallback catalog ensures zero blank states even before DB seed
        return FallbackSummaries
            .Where(c => string.IsNullOrWhiteSpace(category) || string.Equals(c.Category, category, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<CourseDetailDto?> GetCourseAsync(string slug, CancellationToken ct = default)
    {
        using var requestTimeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        requestTimeout.CancelAfter(TimeSpan.FromSeconds(4));
        var res = await _api.GetAsync<CourseDetailDto>($"{ApiRoutes.Public.Courses}/{slug}", requestTimeout.Token);
        if (res is not null) return res;

        var detail = NextGenCoaching.Shared.Catalog.SpecialistPrograms.All.Concat(FallbackDetails)
            .FirstOrDefault(c => string.Equals(c.Slug, slug, StringComparison.OrdinalIgnoreCase));
        if (detail is not null) return detail;
        var summary = FallbackSummaries.FirstOrDefault(c => string.Equals(c.Slug, slug, StringComparison.OrdinalIgnoreCase));
        if (summary is null) return null;
        return new CourseDetailDto
        {
            Id = summary.Id, Slug = summary.Slug, Title = summary.Title, Tagline = summary.Tagline,
            Description = summary.Tagline, DurationMonths = summary.DurationMonths,
            Price = summary.Price, DiscountPrice = summary.DiscountPrice, Currency = summary.Currency,
            Mode = summary.Mode, Level = summary.Level, Category = summary.Category,
            IsFlagship = summary.IsFlagship, SortOrder = summary.SortOrder
        };
    }

    public async Task<IReadOnlyList<string>?> GetCategoriesAsync(CancellationToken ct = default)
    {
        var res = await _api.GetAsync<IReadOnlyList<string>>($"{ApiRoutes.Public.Courses}/categories", ct);
        if (res is { Count: > 0 }) return res;

        return FallbackSummaries.Select(c => c.Category).Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c!).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static IReadOnlyList<CourseSummaryDto> LocalCatalog => FallbackSummaries;

    private static readonly IReadOnlyList<CourseSummaryDto> FallbackSummaries = NextGenCoaching.Shared.Catalog.ProgramCatalog.Programs;

    private static readonly List<CourseDetailDto> FallbackDetails = new()
    {
        new CourseDetailDto
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Slug = "data-science",
            Title = "Data Science & AI Flagship",
            Tagline = "6-month industry-ready program covering SQL, Python, Power BI, Azure Data Factory & Snowflake",
            Description = "A comprehensive 6-month training program designed to transform graduates into industry-ready Data Science & Analytics professionals. Includes live weekend classes, daily mentor war rooms, 6 enterprise capstone projects, bi-weekly mock interviews, and 100% placement support.",
            DurationMonths = 6,
            TotalHours = 240,
            Price = 49999m,
            DiscountPrice = 39999m,
            Currency = "INR",
            Level = "beginner",
            Mode = "live-online",
            Category = "data",
            IsFlagship = true,
            Tools = new List<CourseToolDto>
            {
                new() { ToolName = "SQL", Description = "Querying, window functions, indexing", SortOrder = 1 },
                new() { ToolName = "Python", Description = "Pandas, NumPy, Scikit-learn, ML", SortOrder = 2 },
                new() { ToolName = "Power BI", Description = "DAX, Data modeling, Dashboards", SortOrder = 3 },
                new() { ToolName = "Snowflake", Description = "Cloud data warehousing & queries", SortOrder = 4 },
                new() { ToolName = "Azure Data Factory", Description = "ETL pipelines, triggers, ADF runtimes", SortOrder = 5 }
            },
            Features = new List<CourseFeatureDto>
            {
                new() { Label = "100% Placement Assistance Desk", SortOrder = 1 },
                new() { Label = "6 End-to-End Enterprise Projects", SortOrder = 2 },
                new() { Label = "Bi-Weekly Mock Technical & HR Interviews", SortOrder = 3 },
                new() { Label = "Daily War-Room Mentor Access", SortOrder = 4 }
            },
            JourneySteps = new List<CourseJourneyStepDto>
            {
                new() { StepNumber = 1, Title = "Foundations of SQL & Database Architecture", Description = "Master relational modeling, complex joins, and window functions on production databases." },
                new() { StepNumber = 2, Title = "Python for Analytics & Exploratory Data Analysis", Description = "Clean, wrangle, and analyze multi-gigabyte datasets with Pandas and NumPy." },
                new() { StepNumber = 3, Title = "Business Intelligence & Enterprise Power BI DAX", Description = "Build high-impact executive dashboards and DAX time-intelligence models." },
                new() { StepNumber = 4, Title = "Cloud Warehousing with Snowflake & Azure Data Factory", Description = "Build automated cloud ETL orchestration pipelines from scratch." },
                new() { StepNumber = 5, Title = "Enterprise Capstone War Rooms", Description = "Deliver complete portfolio-grade capstones with live GitHub code reviews." },
                new() { StepNumber = 6, Title = "Resume Polish, Mock Interviews & Placement Drives", Description = "ATS resume audit, 1-on-1 mock interviews, and direct interviews with 200+ partner companies." }
            }
        }
    };
}
