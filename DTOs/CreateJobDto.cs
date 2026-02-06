namespace AuthSystemApi.DTOs
{
    public class CreateJobDto
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string RequiredSkills { get; set; } = "";
        public string ExperienceLevel { get; set; } = "";
        public string EmploymentType { get; set; } = "";
        public string Location { get; set; } = "";
        public string SalaryRange { get; set; } = "";
    }
}
