namespace NextGenCoaching.Shared.Constants;

public static class ApiRoutes
{
    public const string Base = "/api/v1";

    public static class Public
    {
        public const string Courses = Base + "/public/courses";
        public const string Testimonials = Base + "/public/testimonials";
        public const string Faqs = Base + "/public/faqs";
        public const string Webinars = Base + "/public/webinars";
        public const string Enquiries = Base + "/public/enquiries";
        public const string Blog = Base + "/public/blog";
    }

    public static class Auth
    {
        public const string Login = Base + "/auth/login";
        public const string Register = Base + "/auth/register";
        public const string Refresh = Base + "/auth/refresh";
        public const string Logout = Base + "/auth/logout";
        public const string ForgotPassword = Base + "/auth/forgot-password";
        public const string ResetPassword = Base + "/auth/reset-password";
    }

    public static class Admin
    {
        public const string Leads = Base + "/admin/leads";
        public const string Webinars = Base + "/admin/webinars";
        public const string Courses = Base + "/admin/courses";
        public const string Batches = Base + "/admin/batches";
        public const string Students = Base + "/admin/students";
        public const string Reports = Base + "/admin/reports";
        public const string Payments = Base + "/admin/payments";
        public const string Instructors = Base + "/admin/instructors";   // lookup for dropdowns
        public const string Settings = Base + "/admin/settings";
        public const string Placements = Base + "/admin/placements";
        public const string Curriculum = Base + "/admin/curriculum";
        public const string MockInterviews = Base + "/admin/mock-interviews";
        public const string Search = Base + "/admin/search";
        public const string Users = Base + "/admin/users";
    }

    public static class Student
    {
        public const string Me = Base + "/student/me";
        public const string Courses = Base + "/student/courses";
        public const string Sessions = Base + "/student/sessions";
        public const string Assignments = Base + "/student/assignments";
        public const string Projects = Base + "/student/projects";
        public const string MockInterviews = Base + "/student/mock-interviews";
        public const string Dashboard = Base + "/student/dashboard";
        public const string Payments = Base + "/student/payments";
        public const string Profile = Base + "/student/profile";
        public const string Notifications = Base + "/student/notifications";
        public const string Attendance = Base + "/student/attendance";
        public const string Jobs = Base + "/student/jobs";
        public const string Placements = Base + "/student/placements";
        public const string Resumes = Base + "/student/resumes";
    }

    public static class Instructor
    {
        public const string Dashboard          = Base + "/instructor/dashboard";
        public const string Batches            = Base + "/instructor/batches";
        public const string AssignmentsPending = Base + "/instructor/assignments/pending";
        public const string ProjectsPending    = Base + "/instructor/projects/pending";
        public const string Submissions        = Base + "/instructor/submissions";          // /{id}, /{id}/grade
        public const string ProjectSubmissions = Base + "/instructor/project-submissions";  // /{id}, /{id}/grade
        public const string Profile            = Base + "/instructor/profile";
        public const string Attendance         = Base + "/instructor/attendance";            // /sessions, /sessions/{id}
    }

    public static class Hr
    {
        public const string Dashboard       = Base + "/hr/dashboard";
        public const string Resumes         = Base + "/hr/resumes";       // /, /pending, /{id}, /{id}/review
        public const string Jobs            = Base + "/hr/jobs";          // /, /{id}, PUT /{id}, DELETE /{id}
        public const string Referrals       = Base + "/hr/referrals";     // /, /{id}/status
        public const string Profile         = Base + "/hr/profile";
    }
}
