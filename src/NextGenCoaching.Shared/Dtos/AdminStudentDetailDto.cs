namespace NextGenCoaching.Shared.Dtos;

public class AdminStudentDetailDto
{
    // Identity
    public Guid Id { get; init; }            // StudentProfile.Id
    public Guid UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? LastLoginAtUtc { get; init; }

    // Profile
    public string? GraduationDegree { get; init; }
    public int? GraduationYear { get; init; }
    public string? GraduationCollege { get; init; }
    public string? CurrentRole { get; init; }
    public string? TargetRole { get; init; }
    public string? LinkedinUrl { get; init; }
    public string? GithubUrl { get; init; }
    public string? PortfolioUrl { get; init; }
    public string? Bio { get; init; }

    // Rollups
    public int ResumeCount { get; init; }
    public decimal TotalPaid { get; init; }

    public List<AdminStudentEnrollmentDto> Enrollments { get; init; } = new();
    public List<AdminStudentPaymentDto> Payments { get; init; } = new();
}

public class AdminStudentEnrollmentDto
{
    public Guid EnrollmentId { get; init; }
    public string CourseTitle { get; init; } = string.Empty;
    public string BatchName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal AmountPaid { get; init; }
    public decimal CompletionPercentage { get; init; }
    public DateTime EnrolledAtUtc { get; init; }
}

public class AdminStudentPaymentDto
{
    public Guid Id { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "INR";
    public string Status { get; init; } = string.Empty;
    public string? CourseTitle { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
