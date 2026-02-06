namespace AuthSystemApi.DTOs
{
    public class JobApplicationNotificationDto
    {
        public string EmployerEmail { get; set; } = string.Empty;
        public string EmployerName { get; set; } = string.Empty;
        public string JobSeekerName { get; set; } = string.Empty;
        public string JobSeekerEmail { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public DateTime AppliedAt { get; set; }
    }
}