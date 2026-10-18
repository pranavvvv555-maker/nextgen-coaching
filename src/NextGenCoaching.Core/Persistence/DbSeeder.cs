using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NextGenCoaching.Core.Entities;
using NextGenCoaching.Core.Enums;
using NextGenCoaching.Core.Identity;
using NextGenCoaching.Shared.Constants;
using SharedEnums = NextGenCoaching.Shared.Enums;

namespace NextGenCoaching.Core.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        // Apply any pending EF Core migrations, then seed.
        // Schema is now versioned via migrations (see Persistence/Migrations). Adding a table or
        // column means `dotnet ef migrations add <Name>` — no more dropping the database.
        await ctx.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied (if any were pending).");

        await SeedRolesAsync(roleMgr);
        await SeedAdminAsync(userMgr, config, logger);
        await SeedCoursesAsync(ctx);
        await SeedTestimonialsAsync(ctx);
        await SeedFaqsAsync(ctx);
        await SeedEmailTemplatesAsync(ctx);
        await SeedSmsTemplatesAsync(ctx);
        await SeedEmailSettingsAsync(ctx);
        if (config.GetValue<bool>("Seed:IncludeDemoData"))
        {
            await SeedTestStudentAsync(userMgr, ctx, config, logger);
            await SeedTestInstructorAsync(userMgr, ctx, config, logger);
            await SeedTestHrAsync(userMgr, ctx, config, logger);
        }

        logger.LogInformation("Database seeding complete.");
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleMgr)
    {
        foreach (var role in RoleNames.All)
        {
            if (!await roleMgr.RoleExistsAsync(role))
                await roleMgr.CreateAsync(new ApplicationRole(role));
        }
    }

    private static async Task SeedAdminAsync(UserManager<ApplicationUser> userMgr, IConfiguration config, ILogger logger)
    {
        var email = config["Seed:AdminEmail"] ?? "admin@nextgencoaching.local";
        var password = config["Seed:AdminPassword"] ?? "ChangeMe!2026";

        if (await userMgr.FindByEmailAsync(email) is not null) return;

        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = "Platform Admin",
            City = "Mumbai",
            State = "Maharashtra"
        };

        var result = await userMgr.CreateAsync(admin, password);
        if (!result.Succeeded)
        {
            logger.LogError("Admin seed failed: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }
        await userMgr.AddToRoleAsync(admin, RoleNames.Admin);
        logger.LogInformation("Seeded admin user: {Email}", email);
    }

    private static async Task SeedCoursesAsync(AppDbContext ctx)
    {
        if (await ctx.Courses.AnyAsync()) return;

        var courses = new List<Course>
        {
            BuildDataScience(),
            BuildSql(),
            BuildPython(),
            BuildPowerBi(),
            BuildSnowflake(),
            BuildAzureDataFactory()
        };

        foreach (var course in courses)
        {
            var initial = NextGenCoaching.Shared.Catalog.ProgramCatalog.Programs.Single(p => p.Slug == course.Slug);
            course.Id = initial.Id;
            course.Title = initial.Title;
            course.Tagline = initial.Tagline;
            course.DurationMonths = initial.DurationMonths;
            course.Price = initial.Price;
            course.DiscountPrice = initial.DiscountPrice;
            course.Currency = initial.Currency;
            course.Mode = initial.Mode;
            course.Level = initial.Level;
            course.IsFlagship = initial.IsFlagship;
            course.Category = initial.Category;
            course.SortOrder = initial.SortOrder;
        }

        ctx.Courses.AddRange(courses);
        await ctx.SaveChangesAsync();
    }

    // ---------- Course builders ----------

    private static Course BuildDataScience()
    {
        var c = new Course
        {
            Slug = "data-science",
            Title = "Data Science",
            Tagline = "6-month industry-ready program covering SQL, Python, Power BI, Azure Data Factory & Snowflake",
            Description = "A comprehensive 6-month training program designed to transform you into an industry-ready Data Science professional. Live weekend classes, daily practice with mentor support, 6 end-to-end live projects, mock interviews every 15 days, and 100% placement assistance.",
            DurationMonths = 6,
            TotalHours = 240,
            Price = 49999m,
            DiscountPrice = 39999m,
            Currency = "INR",
            Level = "beginner",
            Mode = "hybrid",
            Category = "data",
            IsPublished = true,
            IsFlagship = true,
            SortOrder = 1,
            SeoTitle = "Data Science Training in India | 6-Month Program with Placement",
            SeoDescription = "Industry-ready Data Science training covering Python, SQL, Power BI, Azure Data Factory and Snowflake. Weekend classes, live projects, mock interviews, 100% placement assistance.",
            JourneySteps = TwelveStepJourney(),
            Tools = new List<CourseTool>
            {
                new() { ToolName = "SQL",                 SortOrder = 1, Description = "Querying, joins, window functions, indexing" },
                new() { ToolName = "Python",              SortOrder = 2, Description = "Pandas, NumPy, scikit-learn, ML fundamentals" },
                new() { ToolName = "Power BI",            SortOrder = 3, Description = "DAX, data modelling, dashboards" },
                new() { ToolName = "Azure Data Factory",  SortOrder = 4, Description = "Pipelines, integration runtimes, scheduling" },
                new() { ToolName = "Snowflake",           SortOrder = 5, Description = "Warehousing, time travel, performance tuning" }
            },
            Features = StandardFeatures()
        };

        c.Projects = new List<Project>
        {
            new() { ProjectNumber = 1, Title = "Sales analytics dashboard",      Brief = "Build an end-to-end Power BI sales analytics dashboard from a denormalised source." },
            new() { ProjectNumber = 2, Title = "Customer churn prediction",      Brief = "Train a churn classifier on a telecom dataset and explain key drivers." },
            new() { ProjectNumber = 3, Title = "ETL pipeline with ADF",          Brief = "Move raw transaction data into a star schema warehouse via Azure Data Factory." },
            new() { ProjectNumber = 4, Title = "Snowflake data warehouse",       Brief = "Design and load a multi-source warehouse in Snowflake with quality checks." },
            new() { ProjectNumber = 5, Title = "Recommendation engine",          Brief = "Build a content + collaborative filter recommender for a retail catalogue." },
            new() { ProjectNumber = 6, Title = "Capstone — pick your domain",    Brief = "Choose finance, healthcare, or retail. End-to-end pipeline + ML + dashboard." }
        };

        c.Assignments = FiveAssignmentsPerTool(c.Tools.ToList());

        return c;
    }

    private static Course BuildSql() => new()
    {
        Slug = "sql",
        Title = "SQL",
        Tagline = "6-week intensive on relational data, joins, window functions and performance tuning",
        Description = "Master SQL from the ground up — querying, joins, subqueries, CTEs, window functions, indexing and execution plans. Plenty of hands-on with realistic OLTP and analytical datasets.",
        DurationMonths = 0,
        TotalHours = 60,
        Price = 14999m,
        DiscountPrice = 9999m,
        Mode = "hybrid",
        Category = "data",
        IsPublished = true,
        SortOrder = 2,
        JourneySteps = ShortJourney(new[]
        {
            ("Live webinar",         "Meet the trainer, see the syllabus"),
            ("Enrollment",           "Secure your seat and access materials"),
            ("Foundations",          "Schemas, basic SELECT, filtering, sorting"),
            ("Joins & subqueries",   "Inner/outer joins, correlated subqueries, CTEs"),
            ("Window functions",     "Running totals, ranks, partitioned aggregates"),
            ("Performance",          "Indexes, execution plans, query rewriting"),
            ("Capstone project",     "Build an analytical reporting layer on a real dataset")
        }),
        Tools = new List<CourseTool>
        {
            new() { ToolName = "PostgreSQL", SortOrder = 1 },
            new() { ToolName = "MySQL",      SortOrder = 2 }
        },
        Features = StandardFeatures()
    };

    private static Course BuildPython() => new()
    {
        Slug = "python",
        Title = "Python",
        Tagline = "6-week program from Python basics to data analysis with Pandas and NumPy",
        Description = "Learn Python end-to-end — language fundamentals, data structures, functions, OOP, files, and the data analysis stack (Pandas, NumPy, Matplotlib).",
        DurationMonths = 0,
        TotalHours = 60,
        Price = 14999m,
        DiscountPrice = 9999m,
        Mode = "hybrid",
        Category = "programming",
        IsPublished = true,
        SortOrder = 3,
        JourneySteps = ShortJourney(new[]
        {
            ("Live webinar",      "Trainer intro and roadmap"),
            ("Enrollment",        "Get materials and join the cohort"),
            ("Language basics",   "Variables, control flow, functions"),
            ("Data structures",   "Lists, dicts, sets, comprehensions"),
            ("OOP & modules",     "Classes, packages, virtual environments"),
            ("Pandas & NumPy",    "Data wrangling, cleaning, aggregation"),
            ("Capstone project",  "Build a data analysis report from raw data")
        }),
        Tools = new List<CourseTool>
        {
            new() { ToolName = "Python",     SortOrder = 1 },
            new() { ToolName = "Pandas",     SortOrder = 2 },
            new() { ToolName = "Matplotlib", SortOrder = 3 }
        },
        Features = StandardFeatures()
    };

    private static Course BuildPowerBi() => new()
    {
        Slug = "power-bi",
        Title = "Power BI",
        Tagline = "6-week course on data modelling, DAX, and building business dashboards",
        Description = "Become a confident Power BI developer — data import, transformations, star schemas, DAX measures, dashboards, RLS, and publishing to the service.",
        DurationMonths = 0,
        TotalHours = 60,
        Price = 14999m,
        DiscountPrice = 9999m,
        Mode = "hybrid",
        Category = "analytics",
        IsPublished = true,
        SortOrder = 4,
        JourneySteps = ShortJourney(new[]
        {
            ("Live webinar",        "Trainer intro and live demo"),
            ("Enrollment",          "Get desktop set up"),
            ("Data import & shape", "Power Query, transformations, joins"),
            ("Data modelling",      "Star schema, relationships, granularity"),
            ("DAX",                 "Measures, time intelligence, context transition"),
            ("Dashboards",          "Visualisations, RLS, publishing"),
            ("Capstone project",    "Build a multi-page executive dashboard")
        }),
        Tools = new List<CourseTool>
        {
            new() { ToolName = "Power BI Desktop", SortOrder = 1 },
            new() { ToolName = "Power Query",      SortOrder = 2 },
            new() { ToolName = "DAX",              SortOrder = 3 }
        },
        Features = StandardFeatures()
    };

    private static Course BuildSnowflake() => new()
    {
        Slug = "snowflake",
        Title = "Snowflake",
        Tagline = "4-week deep dive on the Snowflake cloud data platform",
        Description = "Learn Snowflake — architecture, warehouses, storage, time travel, zero-copy clones, role-based access control, performance tuning, and Snowpark basics.",
        DurationMonths = 0,
        TotalHours = 40,
        Price = 19999m,
        DiscountPrice = 14999m,
        Mode = "online",
        Category = "data",
        IsPublished = true,
        SortOrder = 5,
        JourneySteps = ShortJourney(new[]
        {
            ("Live webinar",         "Tour of the platform"),
            ("Enrollment",           "Trial account setup"),
            ("Architecture",         "Compute, storage, services"),
            ("Loading & transform",  "COPY, streams, tasks, Snowpipe"),
            ("Performance & cost",   "Warehouses, clustering, caching"),
            ("Security & RBAC",      "Roles, masking, network policies"),
            ("Capstone project",     "Build a multi-source warehouse")
        }),
        Tools = new List<CourseTool>
        {
            new() { ToolName = "Snowflake",  SortOrder = 1 },
            new() { ToolName = "Snowpipe",   SortOrder = 2 },
            new() { ToolName = "Snowpark",   SortOrder = 3 }
        },
        Features = StandardFeatures()
    };

    private static Course BuildAzureDataFactory() => new()
    {
        Slug = "azure-data-factory",
        Title = "Azure Data Factory",
        Tagline = "6-week course on building cloud-native data pipelines",
        Description = "Build production data pipelines with Azure Data Factory — linked services, datasets, copy activities, mapping data flows, triggers, integration runtimes and monitoring.",
        DurationMonths = 0,
        TotalHours = 60,
        Price = 19999m,
        DiscountPrice = 14999m,
        Mode = "online",
        Category = "cloud",
        IsPublished = true,
        SortOrder = 6,
        JourneySteps = ShortJourney(new[]
        {
            ("Live webinar",          "Azure data services overview"),
            ("Enrollment",            "Azure account setup"),
            ("Pipelines & activities","Copy, Lookup, ForEach, Get Metadata"),
            ("Data flows",            "Visual transformations, debug runs"),
            ("Integration runtimes",  "Self-hosted, Azure, SSIS"),
            ("Triggers & monitoring", "Schedule, tumbling window, event-based"),
            ("Capstone project",      "Build an end-to-end ETL pipeline")
        }),
        Tools = new List<CourseTool>
        {
            new() { ToolName = "Azure Data Factory", SortOrder = 1 },
            new() { ToolName = "Azure SQL",          SortOrder = 2 },
            new() { ToolName = "Azure Blob Storage", SortOrder = 3 }
        },
        Features = StandardFeatures()
    };

    // ---------- Shared content blocks ----------

    private static List<CourseJourneyStep> TwelveStepJourney() => new()
    {
        new() { StepNumber = 1,  Title = "Attend Live Webinar",         Description = "Introduction to the program, meet instructors, Q&A session." },
        new() { StepNumber = 2,  Title = "Register for the Course",     Description = "Official enrollment, payment processing, immediate access to materials." },
        new() { StepNumber = 3,  Title = "Attend Sessions on Weekend",  Description = "Live classroom sessions on weekends with instructor-led training." },
        new() { StepNumber = 4,  Title = "Daily Practice Sessions",     Description = "Hands-on daily practice with dedicated mentor support." },
        new() { StepNumber = 5,  Title = "Mock Interviews Every 15 Days", Description = "Structured mock interview sessions with feedback and improvement suggestions." },
        new() { StepNumber = 6,  Title = "Monthly Industry Expert Connect", Description = "Monthly sessions connecting with industry experts for insights and networking." },
        new() { StepNumber = 7,  Title = "5 Assignments on Each Tool",  Description = "Practical assignments for each technology with assessment and feedback." },
        new() { StepNumber = 8,  Title = "Every Weekend Presentation",  Description = "Weekly presentations to showcase learnings and build confidence." },
        new() { StepNumber = 9,  Title = "6 Live Projects",             Description = "End-to-end project implementation covering real-world scenarios." },
        new() { StepNumber = 10, Title = "Resume Building by HR Experts", Description = "Professional resume creation and LinkedIn optimisation guidance." },
        new() { StepNumber = 11, Title = "Domain Knowledge",            Description = "In-depth industry-specific knowledge and emerging trends." },
        new() { StepNumber = 12, Title = "Call for Interviews",         Description = "Job placement assistance with interview scheduling and company connections." }
    };

    private static List<CourseJourneyStep> ShortJourney(IEnumerable<(string Title, string Description)> steps) =>
        steps.Select((s, i) => new CourseJourneyStep { StepNumber = i + 1, Title = s.Title, Description = s.Description }).ToList();

    private static List<CourseFeature> StandardFeatures() => new()
    {
        new() { Label = "Industrial Projects",        Icon = "rocket",        SortOrder = 1 },
        new() { Label = "Hybrid Training",            Icon = "devices",       SortOrder = 2 },
        new() { Label = "100% Placement Assistance",  Icon = "shield-check",  SortOrder = 3 },
        new() { Label = "Mock Interviews",            Icon = "messages",      SortOrder = 4 },
        new() { Label = "Any Graduates Can Join",     Icon = "school",        SortOrder = 5 },
        new() { Label = "Industry Expert Trainers",   Icon = "user-star",     SortOrder = 6 }
    };

    private static List<Assignment> FiveAssignmentsPerTool(List<CourseTool> tools) =>
        tools.SelectMany(t => Enumerable.Range(1, 5).Select(n => new Assignment
        {
            CourseTool = t,
            AssignmentNumber = n,
            Title = $"{t.ToolName} — Assignment {n}",
            Description = $"Hands-on exercise covering core {t.ToolName} concepts. Submit your solution by the due date for review.",
            DueOffsetDays = n * 14,
            MaxScore = 100
        })).ToList();

    // ---------- Other seed data ----------

    private static async Task SeedTestimonialsAsync(AppDbContext ctx)
    {
        if (await ctx.Testimonials.AnyAsync()) return;
        ctx.Testimonials.AddRange(
            new Testimonial { StudentName = "Priya R.",   CurrentRole = "Power BI Developer",   CurrentCompany = "TCS",         Quote = "I joined as a B.Com graduate with zero coding background. Six months later I had three offers.", IsFeatured = true, SortOrder = 1 },
            new Testimonial { StudentName = "Rohit S.",   CurrentRole = "Data Analyst",         CurrentCompany = "Accenture",   Quote = "The mock interviews were the difference. Every weekend felt like real progress.",        IsFeatured = true, SortOrder = 2 },
            new Testimonial { StudentName = "Sneha P.",   CurrentRole = "Data Engineer",        CurrentCompany = "Cognizant",   Quote = "Industry expert sessions opened doors I didn't know existed.",                            IsFeatured = true, SortOrder = 3 }
        );
        await ctx.SaveChangesAsync();
    }

    private static async Task SeedFaqsAsync(AppDbContext ctx)
    {
        if (await ctx.Faqs.AnyAsync()) return;
        ctx.Faqs.AddRange(
            new Faq { Category = "general",   Question = "Do I need a tech background to join?",       Answer = "No. Any graduate can join. We start from the basics.",                                                SortOrder = 1 },
            new Faq { Category = "general",   Question = "Is this an online or offline program?",      Answer = "We run hybrid programs with live online sessions and optional offline labs at our centres.",          SortOrder = 2 },
            new Faq { Category = "course",    Question = "What if I miss a session?",                  Answer = "Every session is recorded and available within 24 hours.",                                              SortOrder = 3 },
            new Faq { Category = "course",    Question = "Will I get a certificate?",                  Answer = "Yes — on successful completion of all projects you receive an industry-recognised certificate.",        SortOrder = 4 },
            new Faq { Category = "payment",   Question = "Do you offer EMI?",                          Answer = "Yes, no-cost EMI is available on most credit cards.",                                                   SortOrder = 5 },
            new Faq { Category = "placement", Question = "What is your placement support?",            Answer = "Resume reviews, mock interviews, and direct connections to 200+ hiring partners until you're placed.", SortOrder = 6 }
        );
        await ctx.SaveChangesAsync();
    }

    private static async Task SeedEmailTemplatesAsync(AppDbContext ctx)
    {
        var templates = new[]
        {
            new EmailTemplate { Key = "webinar_confirmation", Subject = "You're confirmed for {{webinar_title}}", BodyHtml = "<p>Hi {{full_name}},</p><p>Thanks for registering for our free webinar on {{webinar_title}}. We'll send you the joining link 1 hour before the session.</p>", Description = "Sent immediately after webinar registration." },
            new EmailTemplate { Key = "webinar_reminder_24h", Subject = "Reminder: {{webinar_title}} tomorrow",   BodyHtml = "<p>Hi {{full_name}},</p><p>Just a reminder that the webinar starts tomorrow at {{scheduled_time}}.</p>", Description = "Sent 24h before webinar." },
            new EmailTemplate { Key = "enrollment_welcome",   Subject = "Welcome to NextGen Coaching!",          BodyHtml = "<p>Hi {{full_name}},</p><p>Welcome! Your enrollment in {{course_title}} is confirmed.</p>",            Description = "Sent on successful enrollment." },

            new EmailTemplate { Key = "account_welcome",      Subject = "Welcome to NextGen Coaching!",          BodyHtml = "<p>Hi {{full_name}},</p><p>Your account is ready. Sign in any time to track your courses, assignments, and placement support.</p><p>— The NextGen team</p>", Description = "Sent when a new account is created (public sign-up)." },
            new EmailTemplate { Key = "set_password",         Subject = "Set your NextGen Coaching password",    BodyHtml = "<p>Hi {{full_name}},</p><p>Your student account has been created. Click below to set your password and get started:</p><p><a href=\"{{set_password_url}}\">Set my password</a></p><p>If the link doesn't work, copy this URL into your browser:<br>{{set_password_url}}</p>", Description = "Sent after a counsellor converts a lead to a student." },
            new EmailTemplate { Key = "payment_receipt",      Subject = "Payment received — {{course_title}}",   BodyHtml = "<p>Hi {{full_name}},</p><p>We've received your payment of ₹{{amount}} for {{course_title}}. Your enrolment is confirmed. A detailed invoice is available in your student portal.</p>", Description = "Sent after a successful checkout payment." },
            new EmailTemplate { Key = "assignment_graded",    Subject = "Assignment graded: {{item_title}}",     BodyHtml = "<p>Hi {{full_name}},</p><p>Your assignment <strong>{{item_title}}</strong> has been graded. You scored {{score}} / {{max_score}}. Sign in to read your instructor's feedback.</p>", Description = "Sent when an instructor grades an assignment." },
            new EmailTemplate { Key = "project_graded",       Subject = "Project graded: {{item_title}}",        BodyHtml = "<p>Hi {{full_name}},</p><p>Your project <strong>{{item_title}}</strong> has been graded. You scored {{score}}. Sign in to read your instructor's feedback.</p>", Description = "Sent when an instructor grades a project." },
            new EmailTemplate { Key = "resume_reviewed",      Subject = "Your resume has been {{status}}",       BodyHtml = "<p>Hi {{full_name}},</p><p>Your resume has been {{status}} by our HR team. {{notes}}</p><p>Sign in to your profile to see the details.</p>", Description = "Sent when HR reviews/approves a resume." },
        };

        var existingKeys = await ctx.EmailTemplates.Select(t => t.Key).ToListAsync();
        var missing = templates.Where(t => !existingKeys.Contains(t.Key)).ToList();
        if (missing.Count == 0) return;

        ctx.EmailTemplates.AddRange(missing);
        await ctx.SaveChangesAsync();
    }

    private static async Task SeedSmsTemplatesAsync(AppDbContext ctx)
    {
        if (await ctx.SmsTemplates.AnyAsync()) return;
        ctx.SmsTemplates.AddRange(
            new SmsTemplate { Key = "webinar_reminder_1h", Body = "NextGen: Your free webinar starts in 1 hour. Join here: {{join_url}}" },
            new SmsTemplate { Key = "otp",                 Body = "NextGen: Your OTP is {{otp}}. Do not share." }
        );
        await ctx.SaveChangesAsync();
    }

    // ---------- Email settings (dedicated email_settings table; SMTP config lives here) ----------
    // Idempotent: only inserts the singleton row if it's missing, so admin edits survive restarts.
    private static async Task SeedEmailSettingsAsync(AppDbContext ctx)
    {
        if (await ctx.EmailSettings.AnyAsync(s => s.Id == EmailSetting.SingletonId)) return;

        ctx.EmailSettings.Add(new EmailSetting
        {
            Id = EmailSetting.SingletonId,
            Enabled = false,                              // off by default → emails are logged, not sent
            SmtpHost = "localhost",
            SmtpPort = 587,
            FromEmail = "noreply@nextgencoaching.local",
            FromName = "NextGen Coaching",
            SmtpUser = null,
            SmtpPassword = null,
            UseStartTls = true
        });
        await ctx.SaveChangesAsync();
    }

    // ---------- Test student (so the Student Portal has something to render in dev) ----------
    private static async Task SeedTestStudentAsync(
        UserManager<ApplicationUser> userMgr,
        AppDbContext ctx,
        IConfiguration config,
        ILogger logger)
    {
        var email    = config["Seed:StudentEmail"]    ?? "student@nextgencoaching.local";
        var password = config["Seed:StudentPassword"] ?? "Student123!";

        // Skip if the student already exists
        if (await userMgr.FindByEmailAsync(email) is not null) return;

        // 1. Create the user
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = "Aarav Sharma",
            City = "Bengaluru",
            State = "Karnataka"
        };
        var result = await userMgr.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            logger.LogError("Test student seed failed: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }
        await userMgr.AddToRoleAsync(user, RoleNames.Student);

        // 2. Create the student profile
        var profile = new StudentProfile
        {
            UserId = user.Id,
            GraduationDegree = "B.Sc. Computer Science",
            GraduationYear = 2024,
            GraduationCollege = "Bangalore University",
            CurrentRole = "Recent graduate",
            TargetRole = "Data Analyst",
            LinkedinUrl = "https://linkedin.com/in/aarav-sharma-demo",
            Bio = "Looking to break into data — strong in SQL, learning Python.",
            Status = StudentStatus.Active
        };
        ctx.StudentProfiles.Add(profile);
        await ctx.SaveChangesAsync();

        // 3. Find the Data Science course and create a batch
        var dsCourse = await ctx.Courses.FirstAsync(c => c.Slug == "data-science");
        var batch = new Batch
        {
            CourseId = dsCourse.Id,
            Name = "DS-2026-Spring-Weekend",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            EndDate   = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(150)),
            Mode = SharedEnums.BatchMode.Weekend,
            ScheduleDescription = "Saturdays + Sundays, 10:00–13:00 IST",
            Capacity = 30,
            Status = BatchStatus.Active,
            MeetingRoomUrl = "https://meet.example.com/ds-2026-spring"
        };
        ctx.Batches.Add(batch);
        await ctx.SaveChangesAsync();

        // 4. Enrol the student in this batch
        var enrollment = new Enrollment
        {
            StudentProfileId = profile.Id,
            BatchId = batch.Id,
            EnrolledAtUtc = DateTime.UtcNow.AddDays(-30),
            AmountPaid = dsCourse.DiscountPrice ?? dsCourse.Price,
            Status = SharedEnums.EnrollmentStatus.Active,
            CompletionPercentage = 18.5m
        };
        ctx.Enrollments.Add(enrollment);
        await ctx.SaveChangesAsync();

        // 5. Seed a handful of sessions — some completed, some upcoming
        var sessionTitles = new[]
        {
            ("Kickoff & SQL foundations",          -28, SessionStatus.Completed),
            ("SQL joins & subqueries",             -21, SessionStatus.Completed),
            ("Window functions deep dive",         -14, SessionStatus.Completed),
            ("Indexes & query plans",              -7,  SessionStatus.Completed),
            ("Python fundamentals",                +2,  SessionStatus.Scheduled),
            ("Pandas crash course",                +9,  SessionStatus.Scheduled),
            ("Industry connect: data hiring",      +14, SessionStatus.Scheduled),
            ("Capstone briefing",                  +21, SessionStatus.Scheduled)
        };
        foreach (var (title, dayOffset, status) in sessionTitles)
        {
            ctx.Sessions.Add(new Session
            {
                BatchId = batch.Id,
                Title = title,
                SessionType = SessionType.Lecture,
                ScheduledAtUtc = DateTime.UtcNow.AddDays(dayOffset).Date.AddHours(10),
                DurationMinutes = 90,
                MeetingUrl = "https://meet.example.com/ds-2026-spring/session",
                Status = status
            });
        }
        await ctx.SaveChangesAsync();

        // 6. Submit a couple of early assignments (graded)
        var earlyAssignments = await ctx.Assignments
            .Where(a => a.CourseId == dsCourse.Id)
            .OrderBy(a => a.AssignmentNumber).Take(3).ToListAsync();
        foreach (var (a, i) in earlyAssignments.Select((a, i) => (a, i)))
        {
            ctx.AssignmentSubmissions.Add(new AssignmentSubmission
            {
                AssignmentId = a.Id,
                EnrollmentId = enrollment.Id,
                SubmissionUrl = "https://github.com/aarav-demo/assignment-" + a.AssignmentNumber,
                SubmittedAtUtc = DateTime.UtcNow.AddDays(-20 + i * 3),
                Score = 78 + i * 4,
                Feedback = "Good structure. Watch out for edge cases.",
                Status = SubmissionStatus.Graded,
                GradedAtUtc = DateTime.UtcNow.AddDays(-18 + i * 3)
            });
        }
        await ctx.SaveChangesAsync();

        // 7. One upcoming mock interview
        ctx.MockInterviews.Add(new MockInterview
        {
            EnrollmentId = enrollment.Id,
            RoundNumber = 1,
            ScheduledAtUtc = DateTime.UtcNow.AddDays(10).Date.AddHours(16),
            DurationMinutes = 60,
            MeetingUrl = "https://meet.example.com/mock/aarav-r1",
            Status = MockInterviewStatus.Scheduled
        });

        // 8. Two announcements (Notification rows targeted at this user)
        ctx.Notifications.AddRange(
            new Notification
            {
                UserId = user.Id,
                Type = "Announcement",
                Title = "Capstone briefing this week",
                Message = "Capstone topics will be released on Sunday — pick yours by EOD Tuesday.",
                LinkUrl = "/student/courses",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
            },
            new Notification
            {
                UserId = user.Id,
                Type = "Announcement",
                Title = "New industry expert session added",
                Message = "Senior DE from Flipkart joining us for an industry connect on the 14th.",
                LinkUrl = "/student/sessions",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-3)
            }
        );

        await ctx.SaveChangesAsync();
        logger.LogInformation("Seeded test student: {Email} (password: {Password})", email, password);
    }

    // ---------- Test instructor (so the Instructor portal has grading work to do) ----------
    private static async Task SeedTestInstructorAsync(
        UserManager<ApplicationUser> userMgr,
        AppDbContext ctx,
        IConfiguration config,
        ILogger logger)
    {
        var email    = config["Seed:InstructorEmail"]    ?? "instructor@nextgencoaching.local";
        var password = config["Seed:InstructorPassword"] ?? "Instructor123!";

        if (await userMgr.FindByEmailAsync(email) is not null) return;

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = "Priya Iyer",
            City = "Bengaluru",
            State = "Karnataka"
        };
        var result = await userMgr.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            logger.LogError("Test instructor seed failed: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }
        await userMgr.AddToRoleAsync(user, RoleNames.Instructor);

        var profile = new InstructorProfile
        {
            UserId = user.Id,
            Headline = "Lead Data Engineer · ex-Flipkart, ex-Swiggy",
            Bio = "10+ years across data platforms. Loves teaching SQL and Snowflake.",
            ExpertiseTags = new List<string> { "SQL", "Python", "Snowflake", "ADF" },
            YearsExperience = 11,
            LinkedinUrl = "https://linkedin.com/in/priya-iyer-demo",
            IsPublic = true
        };
        ctx.InstructorProfiles.Add(profile);
        await ctx.SaveChangesAsync();

        // Assign Aarav's Data Science batch to this instructor so they have students + submissions to grade
        var dsBatch = await ctx.Batches.FirstOrDefaultAsync(b => b.Name == "DS-2026-Spring-Weekend");
        if (dsBatch is not null && dsBatch.PrimaryInstructorId is null)
        {
            dsBatch.PrimaryInstructorId = profile.Id;
            await ctx.SaveChangesAsync();
        }

        logger.LogInformation("Seeded test instructor: {Email} (password: {Password})", email, password);
    }

    // ---------- Test HR reviewer + sample resume/jobs/referral ----------
    private static async Task SeedTestHrAsync(
        UserManager<ApplicationUser> userMgr,
        AppDbContext ctx,
        IConfiguration config,
        ILogger logger)
    {
        var email    = config["Seed:HrEmail"]    ?? "hr@nextgencoaching.local";
        var password = config["Seed:HrPassword"] ?? "HrUser123!";

        if (await userMgr.FindByEmailAsync(email) is not null) return;

        // 1. Create the HR user
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = "Meera Krishnan",
            City = "Bengaluru",
            State = "Karnataka"
        };
        var result = await userMgr.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            logger.LogError("Test HR seed failed: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }
        await userMgr.AddToRoleAsync(user, RoleNames.HrReviewer);

        // 2. Seed a pending resume for Aarav so HR has something to review
        var aaravProfile = await ctx.StudentProfiles
            .FirstOrDefaultAsync(p => p.User != null && p.User.Email == "student@nextgencoaching.local");
        if (aaravProfile is not null && !await ctx.Resumes.AnyAsync(r => r.StudentProfileId == aaravProfile.Id))
        {
            ctx.Resumes.Add(new Resume
            {
                StudentProfileId = aaravProfile.Id,
                Version = 1,
                FileUrl = "https://drive.example.com/aarav-resume-v1.pdf",
                Status = ResumeStatus.UnderReview,
                IsCurrent = true
            });
        }

        // 3. Two sample job openings
        var existingJobsCount = await ctx.JobOpenings.CountAsync();
        if (existingJobsCount == 0)
        {
            ctx.JobOpenings.AddRange(
                new JobOpening
                {
                    CompanyName = "Cobalt Analytics",
                    RoleTitle = "Junior Data Analyst",
                    Description = "Entry-level role on the BI team. SQL-heavy, with Python for ad-hoc analysis. Mentoring from senior analysts.",
                    SkillsRequired = new List<string> { "SQL", "Excel", "Python", "Power BI" },
                    MinExperienceYears = 0,
                    MinCtcLpa = 6, MaxCtcLpa = 9,
                    Location = "Bengaluru",
                    WorkMode = "Hybrid",
                    Status = JobOpeningStatus.Open,
                    PostedAtUtc = DateTime.UtcNow.AddDays(-3),
                    ApplicationUrl = "https://cobalt.example.com/careers/jda-2026"
                },
                new JobOpening
                {
                    CompanyName = "Northwind Logistics",
                    RoleTitle = "Data Engineer Intern",
                    Description = "6-month internship on the data platform team. Strong chance of full-time conversion. Snowflake + ADF stack.",
                    SkillsRequired = new List<string> { "Python", "SQL", "Snowflake", "ADF" },
                    MinExperienceYears = 0,
                    MinCtcLpa = 4, MaxCtcLpa = 5,
                    Location = "Remote",
                    WorkMode = "Remote",
                    Status = JobOpeningStatus.Open,
                    PostedAtUtc = DateTime.UtcNow.AddDays(-7),
                    ApplicationUrl = "https://nwlogistics.example.com/careers/de-intern"
                }
            );
            await ctx.SaveChangesAsync();
        }

        // 4. One sample referral so the referrals page isn't empty
        if (aaravProfile is not null && !await ctx.JobReferrals.AnyAsync(r => r.StudentProfileId == aaravProfile.Id))
        {
            var firstJob = await ctx.JobOpenings.FirstOrDefaultAsync(j => j.CompanyName == "Cobalt Analytics");
            if (firstJob is not null)
            {
                ctx.JobReferrals.Add(new JobReferral
                {
                    JobOpeningId = firstJob.Id,
                    StudentProfileId = aaravProfile.Id,
                    ReferredByUserId = user.Id,
                    ReferredAtUtc = DateTime.UtcNow.AddDays(-2),
                    Status = JobReferralStatus.Screening,
                    StatusUpdatedAtUtc = DateTime.UtcNow.AddDays(-1),
                    Notes = "Strong SQL fundamentals from the DS course. Worth a screen."
                });
            }
        }

        await ctx.SaveChangesAsync();
        logger.LogInformation("Seeded test HR reviewer: {Email} (password: {Password})", email, password);
    }
}
