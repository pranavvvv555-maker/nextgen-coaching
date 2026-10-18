using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NextGenCoaching.Core.Common;
using NextGenCoaching.Core.Entities;
using NextGenCoaching.Core.Identity;

namespace NextGenCoaching.Core.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Catalogue
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseTool> CourseTools => Set<CourseTool>();
    public DbSet<CourseJourneyStep> CourseJourneySteps => Set<CourseJourneyStep>();
    public DbSet<CourseFeature> CourseFeatures => Set<CourseFeature>();

    // People
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<InstructorProfile> InstructorProfiles => Set<InstructorProfile>();
    public DbSet<CounsellorProfile> CounsellorProfiles => Set<CounsellorProfile>();

    // Marketing / funnel
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<LeadActivity> LeadActivities => Set<LeadActivity>();
    public DbSet<ContactEnquiry> ContactEnquiries => Set<ContactEnquiry>();
    public DbSet<Webinar> Webinars => Set<Webinar>();
    public DbSet<WebinarRegistration> WebinarRegistrations => Set<WebinarRegistration>();

    // Operations
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<SessionAttendance> SessionAttendances => Set<SessionAttendance>();
    public DbSet<IndustryConnect> IndustryConnects => Set<IndustryConnect>();

    // Assignments / projects
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<AssignmentSubmission> AssignmentSubmissions => Set<AssignmentSubmission>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectSubmission> ProjectSubmissions => Set<ProjectSubmission>();
    public DbSet<MockInterview> MockInterviews => Set<MockInterview>();

    // Career
    public DbSet<Resume> Resumes => Set<Resume>();
    public DbSet<JobOpening> JobOpenings => Set<JobOpening>();
    public DbSet<JobReferral> JobReferrals => Set<JobReferral>();
    public DbSet<Placement> Placements => Set<Placement>();

    // CMS-lite
    public DbSet<Testimonial> Testimonials => Set<Testimonial>();
    public DbSet<Faq> Faqs => Set<Faq>();
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<BlogTag> BlogTags => Set<BlogTag>();
    public DbSet<BlogPostTag> BlogPostTags => Set<BlogPostTag>();

    // System
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<SmsTemplate> SmsTemplates => Set<SmsTemplate>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<EmailSetting> EmailSettings => Set<EmailSetting>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Identity table names — keep them snake_case to match the rest
        b.Entity<ApplicationUser>(e =>
        {
            e.ToTable("users");
            e.Property(u => u.FullName).HasMaxLength(150).IsRequired();
            e.Property(u => u.City).HasMaxLength(100);
            e.Property(u => u.State).HasMaxLength(100);
            e.Property(u => u.ProfileImageUrl).HasMaxLength(500);
            e.Property(u => u.RefreshToken).HasMaxLength(200);
        });
        b.Entity<ApplicationRole>().ToTable("roles");
        b.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>().ToTable("user_roles");
        b.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<Guid>>().ToTable("user_claims");
        b.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<Guid>>().ToTable("user_logins");
        b.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<Guid>>().ToTable("role_claims");
        b.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<Guid>>().ToTable("user_tokens");

        // Course
        b.Entity<Course>(e =>
        {
            e.HasIndex(c => c.Slug).IsUnique();
            e.Property(c => c.Slug).HasMaxLength(150).IsRequired();
            e.Property(c => c.Title).HasMaxLength(200).IsRequired();
            e.Property(c => c.Tagline).HasMaxLength(300);
            e.Property(c => c.Currency).HasMaxLength(3);
            e.Property(c => c.Level).HasMaxLength(20);
            e.Property(c => c.Mode).HasMaxLength(20);
            e.Property(c => c.HeroImageUrl).HasMaxLength(500);
            e.Property(c => c.BrochureUrl).HasMaxLength(500);
            e.Property(c => c.Category).HasMaxLength(40);
            e.Property(c => c.SeoTitle).HasMaxLength(200);
            e.Property(c => c.SeoDescription).HasMaxLength(500);
            e.Property(c => c.Price).HasPrecision(10, 2);
            e.Property(c => c.DiscountPrice).HasPrecision(10, 2);
        });

        b.Entity<CourseJourneyStep>(e =>
        {
            e.HasIndex(s => new { s.CourseId, s.StepNumber }).IsUnique();
            e.Property(s => s.Title).HasMaxLength(200).IsRequired();
            e.Property(s => s.Icon).HasMaxLength(50);
        });

        b.Entity<CourseTool>(e =>
        {
            e.Property(t => t.ToolName).HasMaxLength(100).IsRequired();
            e.Property(t => t.IconUrl).HasMaxLength(500);
        });

        b.Entity<CourseFeature>(e =>
        {
            e.Property(f => f.Label).HasMaxLength(150).IsRequired();
            e.Property(f => f.Icon).HasMaxLength(50);
        });

        // People
        b.Entity<StudentProfile>(e =>
        {
            e.HasIndex(p => p.UserId).IsUnique();
            e.Property(p => p.GraduationDegree).HasMaxLength(100);
            e.Property(p => p.GraduationCollege).HasMaxLength(200);
            e.Property(p => p.CurrentRole).HasMaxLength(100);
            e.Property(p => p.TargetRole).HasMaxLength(100);
            e.Property(p => p.LinkedinUrl).HasMaxLength(300);
            e.Property(p => p.GithubUrl).HasMaxLength(300);
            e.Property(p => p.PortfolioUrl).HasMaxLength(300);
            e.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        });
        b.Entity<InstructorProfile>(e =>
        {
            e.HasIndex(p => p.UserId).IsUnique();
            e.Property(p => p.Headline).HasMaxLength(200);
            e.Property(p => p.LinkedinUrl).HasMaxLength(300);
            e.Property(p => p.HourlyRate).HasPrecision(10, 2);
        });
        b.Entity<CounsellorProfile>().HasIndex(p => p.UserId).IsUnique();

        // Lead
        b.Entity<Lead>(e =>
        {
            e.HasIndex(l => l.Email);
            e.HasIndex(l => l.Phone);
            e.HasIndex(l => new { l.Status, l.AssignedCounsellorId, l.NextFollowupAtUtc });
            e.Property(l => l.FullName).HasMaxLength(150).IsRequired();
            e.Property(l => l.Email).HasMaxLength(256).IsRequired();
            e.Property(l => l.Phone).HasMaxLength(20).IsRequired();
            e.Property(l => l.City).HasMaxLength(100);
            e.Property(l => l.Graduation).HasMaxLength(150);
            e.Property(l => l.Source).HasConversion<string>().HasMaxLength(30);
            e.Property(l => l.SourceCampaign).HasMaxLength(100);
            e.Property(l => l.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(l => l.LostReason).HasMaxLength(200);
        });
        b.Entity<LeadActivity>(e =>
        {
            e.HasIndex(a => a.LeadId);
            e.Property(a => a.ActivityType).HasConversion<string>().HasMaxLength(30);
            e.Property(a => a.Outcome).HasMaxLength(50);
        });
        b.Entity<ContactEnquiry>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(150).IsRequired();
            e.Property(c => c.Email).HasMaxLength(256).IsRequired();
            e.Property(c => c.Phone).HasMaxLength(20);
            e.Property(c => c.Subject).HasMaxLength(200);
            e.Property(c => c.SourcePage).HasMaxLength(200);
        });

        // Webinar
        b.Entity<Webinar>(e =>
        {
            e.HasIndex(w => w.ScheduledAtUtc);
            e.Property(w => w.Title).HasMaxLength(200).IsRequired();
            e.Property(w => w.JoinUrl).HasMaxLength(500);
            e.Property(w => w.RecordingUrl).HasMaxLength(500);
            e.Property(w => w.BannerImageUrl).HasMaxLength(500);
            e.Property(w => w.Status).HasConversion<string>().HasMaxLength(20);
        });
        b.Entity<WebinarRegistration>(e =>
        {
            e.HasIndex(r => new { r.WebinarId, r.LeadId }).IsUnique();
        });

        // Batches & sessions
        b.Entity<Batch>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.ScheduleDescription).HasMaxLength(300);
            e.Property(x => x.MeetingRoomUrl).HasMaxLength(500);
            e.Property(x => x.Mode).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        });
        b.Entity<Enrollment>(e =>
        {
            e.HasIndex(en => new { en.StudentProfileId, en.BatchId }).IsUnique();
            e.Property(en => en.AmountPaid).HasPrecision(10, 2);
            e.Property(en => en.CompletionPercentage).HasPrecision(5, 2);
            e.Property(en => en.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(en => en.CertificateUrl).HasMaxLength(500);
        });
        b.Entity<Session>(e =>
        {
            e.HasIndex(s => new { s.BatchId, s.ScheduledAtUtc });
            e.Property(s => s.Title).HasMaxLength(200).IsRequired();
            e.Property(s => s.MeetingUrl).HasMaxLength(500);
            e.Property(s => s.RecordingUrl).HasMaxLength(500);
            e.Property(s => s.ResourcesUrl).HasMaxLength(500);
            e.Property(s => s.SessionType).HasConversion<string>().HasMaxLength(30);
            e.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        });
        b.Entity<SessionAttendance>(e =>
        {
            e.HasIndex(a => new { a.SessionId, a.EnrollmentId }).IsUnique();
            e.Property(a => a.Status).HasConversion<string>().HasMaxLength(10);
        });
        b.Entity<IndustryConnect>(e =>
        {
            e.Property(i => i.Title).HasMaxLength(200).IsRequired();
            e.Property(i => i.ExpertName).HasMaxLength(150).IsRequired();
            e.Property(i => i.ExpertCompany).HasMaxLength(150);
            e.Property(i => i.ExpertDesignation).HasMaxLength(150);
            e.Property(i => i.ExpertLinkedin).HasMaxLength(300);
            e.Property(i => i.ExpertPhotoUrl).HasMaxLength(500);
            e.Property(i => i.MeetingUrl).HasMaxLength(500);
            e.Property(i => i.RecordingUrl).HasMaxLength(500);
        });

        // Assignments / projects / mock interviews
        b.Entity<Assignment>(e =>
        {
            e.Property(a => a.Title).HasMaxLength(200).IsRequired();
            e.Property(a => a.ResourcesUrl).HasMaxLength(500);
        });
        b.Entity<AssignmentSubmission>(e =>
        {
            e.HasIndex(s => new { s.AssignmentId, s.EnrollmentId }).IsUnique();
            e.Property(s => s.SubmissionUrl).HasMaxLength(500);
            e.Property(s => s.Score).HasPrecision(5, 2);
            e.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        });
        b.Entity<Project>(e =>
        {
            e.HasIndex(p => new { p.CourseId, p.ProjectNumber }).IsUnique();
            e.Property(p => p.Title).HasMaxLength(200).IsRequired();
            e.Property(p => p.DatasetUrl).HasMaxLength(500);
            e.Property(p => p.RubricJson).HasColumnType("jsonb");
        });
        b.Entity<ProjectSubmission>(e =>
        {
            e.HasIndex(s => new { s.ProjectId, s.EnrollmentId }).IsUnique();
            e.Property(s => s.RepoUrl).HasMaxLength(500);
            e.Property(s => s.DeployedUrl).HasMaxLength(500);
            e.Property(s => s.PresentationUrl).HasMaxLength(500);
            e.Property(s => s.EvaluationJson).HasColumnType("jsonb");
            e.Property(s => s.TotalScore).HasPrecision(5, 2);
            e.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        });
        b.Entity<MockInterview>(e =>
        {
            e.Property(m => m.MeetingUrl).HasMaxLength(500);
            e.Property(m => m.RecordingUrl).HasMaxLength(500);
            e.Property(m => m.Status).HasConversion<string>().HasMaxLength(20);
        });

        // Career
        b.Entity<Resume>(e =>
        {
            e.Property(r => r.FileUrl).HasMaxLength(500).IsRequired();
            e.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        });
        b.Entity<JobOpening>(e =>
        {
            e.Property(j => j.CompanyName).HasMaxLength(200).IsRequired();
            e.Property(j => j.CompanyLogoUrl).HasMaxLength(500);
            e.Property(j => j.RoleTitle).HasMaxLength(200).IsRequired();
            e.Property(j => j.Location).HasMaxLength(150);
            e.Property(j => j.WorkMode).HasMaxLength(20);
            e.Property(j => j.ApplicationUrl).HasMaxLength(500);
            e.Property(j => j.MinExperienceYears).HasPrecision(3, 1);
            e.Property(j => j.MinCtcLpa).HasPrecision(6, 2);
            e.Property(j => j.MaxCtcLpa).HasPrecision(6, 2);
            e.Property(j => j.Status).HasConversion<string>().HasMaxLength(20);
        });
        b.Entity<JobReferral>(e =>
        {
            e.Property(j => j.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(j => j.OfferedCtcLpa).HasPrecision(6, 2);
        });
        b.Entity<Placement>(e =>
        {
            // PER-COURSE placement: a student can be placed once per course they complete
            e.HasIndex(p => new { p.StudentProfileId, p.CourseId }).IsUnique();
            e.Property(p => p.CompanyName).HasMaxLength(200).IsRequired();
            e.Property(p => p.CompanyLogoUrl).HasMaxLength(500);
            e.Property(p => p.RoleTitle).HasMaxLength(200).IsRequired();
            e.Property(p => p.CtcLpa).HasPrecision(6, 2);
        });

        // CMS
        b.Entity<Testimonial>(e =>
        {
            e.HasIndex(t => t.IsFeatured);
            e.Property(t => t.StudentName).HasMaxLength(150).IsRequired();
            e.Property(t => t.PhotoUrl).HasMaxLength(500);
            e.Property(t => t.CurrentRole).HasMaxLength(150);
            e.Property(t => t.CurrentCompany).HasMaxLength(150);
            e.Property(t => t.BatchName).HasMaxLength(100);
            e.Property(t => t.VideoUrl).HasMaxLength(500);
        });
        b.Entity<Faq>(e =>
        {
            e.Property(f => f.Category).HasMaxLength(50);
            e.Property(f => f.Question).HasMaxLength(500).IsRequired();
        });
        b.Entity<BlogPost>(e =>
        {
            e.HasIndex(p => p.Slug).IsUnique();
            e.HasIndex(p => p.PublishedAtUtc);
            e.Property(p => p.Slug).HasMaxLength(200).IsRequired();
            e.Property(p => p.Title).HasMaxLength(300).IsRequired();
            e.Property(p => p.Excerpt).HasMaxLength(500);
            e.Property(p => p.CoverImageUrl).HasMaxLength(500);
            e.Property(p => p.SeoTitle).HasMaxLength(200);
            e.Property(p => p.SeoDescription).HasMaxLength(500);
        });
        b.Entity<BlogTag>(e =>
        {
            e.HasIndex(t => t.Slug).IsUnique();
            e.Property(t => t.Slug).HasMaxLength(50).IsRequired();
            e.Property(t => t.Name).HasMaxLength(50).IsRequired();
        });
        b.Entity<BlogPostTag>(e =>
        {
            e.HasKey(pt => new { pt.BlogPostId, pt.BlogTagId });
            e.HasOne(pt => pt.BlogPost).WithMany(p => p.PostTags).HasForeignKey(pt => pt.BlogPostId);
            e.HasOne(pt => pt.BlogTag).WithMany(t => t.PostTags).HasForeignKey(pt => pt.BlogTagId);
        });

        // System
        b.Entity<EmailTemplate>(e =>
        {
            e.HasIndex(t => t.Key).IsUnique();
            e.Property(t => t.Key).HasMaxLength(80).IsRequired();
            e.Property(t => t.Subject).HasMaxLength(300).IsRequired();
        });
        b.Entity<SmsTemplate>(e =>
        {
            e.HasIndex(t => t.Key).IsUnique();
            e.Property(t => t.Key).HasMaxLength(80).IsRequired();
            e.Property(t => t.Body).HasMaxLength(500).IsRequired();
            e.Property(t => t.DltTemplateId).HasMaxLength(50);
        });
        b.Entity<Notification>(e =>
        {
            e.HasIndex(n => new { n.UserId, n.CreatedAtUtc });
            e.Property(n => n.Type).HasMaxLength(50).IsRequired();
            e.Property(n => n.Title).HasMaxLength(200).IsRequired();
            e.Property(n => n.Message).HasMaxLength(500);
            e.Property(n => n.LinkUrl).HasMaxLength(500);
            e.Property(n => n.PayloadJson).HasColumnType("jsonb");
        });
        b.Entity<PaymentTransaction>(e =>
        {
            e.HasIndex(p => p.ProviderOrderId).IsUnique();
            e.Property(p => p.Provider).HasMaxLength(20).IsRequired();
            e.Property(p => p.ProviderOrderId).HasMaxLength(100).IsRequired();
            e.Property(p => p.ProviderPaymentId).HasMaxLength(100);
            e.Property(p => p.Amount).HasPrecision(10, 2);
            e.Property(p => p.Currency).HasMaxLength(3);
            e.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.RawResponseJson).HasColumnType("jsonb");
            e.Property(p => p.RefundReason).HasMaxLength(300);
        });
        b.Entity<AuditLog>(e =>
        {
            e.HasIndex(a => new { a.EntityName, a.EntityId, a.CreatedAtUtc });
            e.Property(a => a.Action).HasMaxLength(50).IsRequired();
            e.Property(a => a.EntityName).HasMaxLength(100).IsRequired();
            e.Property(a => a.OldValuesJson).HasColumnType("jsonb");
            e.Property(a => a.NewValuesJson).HasColumnType("jsonb");
            e.Property(a => a.IpAddress).HasMaxLength(45);
            e.Property(a => a.UserAgent).HasMaxLength(500);
        });
        b.Entity<AppSetting>(e =>
        {
            e.HasKey(s => s.Key);
            e.Property(s => s.Key).HasMaxLength(100);
            e.Property(s => s.Description).HasMaxLength(300);
        });
        b.Entity<EmailSetting>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).ValueGeneratedNever();
            e.Property(s => s.SmtpHost).HasMaxLength(255);
            e.Property(s => s.FromEmail).HasMaxLength(255);
            e.Property(s => s.FromName).HasMaxLength(255);
            e.Property(s => s.SmtpUser).HasMaxLength(255);
            e.Property(s => s.SmtpPassword).HasMaxLength(512);
        });

        // Soft-delete global filter for everything that implements ISoftDeletable
        foreach (var entityType in b.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType)) continue;

            var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
            var prop = System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
            var notDeleted = System.Linq.Expressions.Expression.Equal(prop, System.Linq.Expressions.Expression.Constant(false));
            var lambda = System.Linq.Expressions.Expression.Lambda(notDeleted, parameter);
            b.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }
}
