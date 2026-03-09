namespace AuthSystemApi.DTOs
{
    public class ResumeParseRequestDto
    {
        public string ResumeText { get; set; } = "";
    }

    public class ResumeParseResponseDto
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Location { get; set; }
        public int? TotalExperienceYears { get; set; }
        public string? CurrentJobTitle { get; set; }
        public List<string> Skills { get; set; } = new();
        public List<EducationDto> Education { get; set; } = new();
        public List<WorkExperienceDto> WorkExperience { get; set; } = new();
        public List<string> Certifications { get; set; } = new();
    }

    public class EducationDto
    {
        public string? Degree { get; set; }
        public string? Field { get; set; }
        public string? Institution { get; set; }
        public string? Year { get; set; }
    }

    public class WorkExperienceDto
    {
        public string? JobTitle { get; set; }
        public string? Company { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public List<string> TechnologiesUsed { get; set; } = new();
    }
}
