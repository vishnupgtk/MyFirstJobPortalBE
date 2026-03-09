namespace AuthSystemApi.DTOs
{
    public class EmployerDashboardMetricsDto
    {
        public int ActiveJobs { get; set; }
        public int TotalApplicants { get; set; }
        public int NewApplicants { get; set; }
        public int ShortlistedCandidates { get; set; }
        public int InterviewsScheduled { get; set; }
    }

    public class DashboardJobDto
    {
        public int JobId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int ApplicantCount { get; set; }
        public int NewApplicantCount { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; }
    }

    public class RecentApplicantDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public int JobId { get; set; }
        public DateTime AppliedAt { get; set; }
        public string Status { get; set; } = "Pending";
    }
}
