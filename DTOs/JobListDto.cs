namespace AuthSystemApi.DTOs
{
    public class JobListDto
    {
        public int JobId { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string RequiredSkills { get; set; } = "";
        public string Location { get; set; } = "";
        public string EmploymentType { get; set; } = "";
        public string PostedBy { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public int ApplicantCount { get; set; }

        // For job seeker applications
        public DateTime? AppliedAt { get; set; }
        public string Status { get; set; } = "";
    }
}
