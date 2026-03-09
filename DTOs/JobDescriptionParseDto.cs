namespace AuthSystemApi.DTOs
{
    public class JobDescriptionParseRequestDto
    {
        public string JobDescriptionText { get; set; } = "";
    }

    public class JobDescriptionParseResponseDto
    {
        public string? JobTitle { get; set; }
        public List<string> RequiredSkills { get; set; } = new();
        public List<string> PreferredSkills { get; set; } = new();
        public int? MinimumExperienceYears { get; set; }
        public string? EducationRequirement { get; set; }
        public string? Location { get; set; }
        public string? Industry { get; set; }
        public string? SeniorityLevel { get; set; }
    }
}
