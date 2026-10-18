using NextGenCoaching.Shared.Dtos;

namespace NextGenCoaching.Shared.Catalog;

/// <summary>Initial catalogue shared by database seeding and the offline website preview. Admin edits remain authoritative once seeded.</summary>
public static class ProgramCatalog
{
    public static IReadOnlyList<CourseSummaryDto> Programs { get; } = new List<CourseSummaryDto>()
    {
        new CourseSummaryDto
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Slug = "data-science",
            Title = "Data Science & AI Flagship",
            Tagline = "6-month industry-ready program covering SQL, Python, Power BI, Azure Data Factory & Snowflake",
            DurationMonths = 6,
            Price = 49999m,
            DiscountPrice = 39999m,
            Currency = "INR",
            Mode = "live-online",
            Level = "beginner",
            IsFlagship = true,
            Category = "data",
            SortOrder = 1
        },
        new CourseSummaryDto
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Slug = "sql",
            Title = "SQL for Analytics & Data Engineering",
            Tagline = "Master complex joins, window functions, CTEs, indexing, and query tuning with production schemas",
            DurationMonths = 1,
            Price = 12999m,
            DiscountPrice = 9999m,
            Currency = "INR",
            Mode = "live-online",
            Level = "beginner",
            IsFlagship = false,
            Category = "data",
            SortOrder = 2
        },
        new CourseSummaryDto
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Slug = "python",
            Title = "Python for Data & Automation",
            Tagline = "Hands-on data analysis, Pandas, NumPy, scikit-learn, and API automations from scratch",
            DurationMonths = 2,
            Price = 18999m,
            DiscountPrice = 14999m,
            Currency = "INR",
            Mode = "live-online",
            Level = "beginner",
            IsFlagship = false,
            Category = "data",
            SortOrder = 3
        },
        new CourseSummaryDto
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Slug = "power-bi",
            Title = "Power BI & Business Intelligence",
            Tagline = "Enterprise DAX modeling, interactive report storytelling, Power Query, and service publishing",
            DurationMonths = 2,
            Price = 18999m,
            DiscountPrice = 14999m,
            Currency = "INR",
            Mode = "live-online",
            Level = "intermediate",
            IsFlagship = false,
            Category = "bi",
            SortOrder = 4
        },
        new CourseSummaryDto
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            Slug = "snowflake",
            Title = "Snowflake Cloud Data Warehousing",
            Tagline = "Modern cloud data warehousing, virtual warehouses, zero-copy cloning, time travel, and Streams",
            DurationMonths = 2,
            Price = 24999m,
            DiscountPrice = 19999m,
            Currency = "INR",
            Mode = "live-online",
            Level = "intermediate",
            IsFlagship = false,
            Category = "cloud",
            SortOrder = 5
        },
        new CourseSummaryDto
        {
            Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
            Slug = "azure-data-factory",
            Title = "Azure Data Factory (ADF) & Cloud ETL",
            Tagline = "Production data orchestration, pipeline monitoring, mapping data flows, and lakehouse integration",
            DurationMonths = 2,
            Price = 24999m,
            DiscountPrice = 19999m,
            Currency = "INR",
            Mode = "live-online",
            Level = "intermediate",
            IsFlagship = false,
            Category = "cloud",
            SortOrder = 6
        }
    }.AsReadOnly();
}
