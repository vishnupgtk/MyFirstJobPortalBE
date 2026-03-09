namespace AuthSystemApi.DTOs
{
    public class MatchingScoreRequestDto
    {
        public ResumeParseResponseDto Resume { get; set; } = new();
        public JobDescriptionParseResponseDto JobDescription { get; set; } = new();
    }

    public class MatchingScoreResponseDto
    {
        public double MatchPercentage { get; set; }
        public SkillMatchDto SkillMatch { get; set; } = new();
        public ExperienceMatchDto ExperienceMatch { get; set; } = new();
        public EducationMatchDto EducationMatch { get; set; } = new();
        public LocationMatchDto LocationMatch { get; set; } = new();
    }

    public class SkillMatchDto
    {
        public double Score { get; set; }
        public List<string> MatchedRequiredSkills { get; set; } = new();
        public List<string> MissingRequiredSkills { get; set; } = new();
        public List<string> MatchedPreferredSkills { get; set; } = new();
    }

    public class ExperienceMatchDto
    {
        public double Score { get; set; }
        public int? CandidateYears { get; set; }
        public int? RequiredYears { get; set; }
    }

    public class EducationMatchDto
    {
        public double Score { get; set; }
        public bool Matches { get; set; }
    }

    public class LocationMatchDto
    {
        public double Score { get; set; }
        public bool Matches { get; set; }
    }

    public class JobApplicantWithScoreDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Skills { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime AppliedAt { get; set; }
        public double? MatchPercentage { get; set; }
        public MatchingScoreResponseDto? MatchDetails { get; set; }
    }
}
