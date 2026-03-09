using AuthSystemApi.DTOs;

namespace AuthSystemApi.Services
{
    public class MatchingService
    {
        public MatchingScoreResponseDto CalculateMatchScore(ResumeParseResponseDto resume, JobDescriptionParseResponseDto jobDescription)
        {
            var result = new MatchingScoreResponseDto();

            result.SkillMatch = CalculateSkillMatch(resume.Skills, jobDescription.RequiredSkills, jobDescription.PreferredSkills);
            result.ExperienceMatch = CalculateExperienceMatch(resume.TotalExperienceYears, jobDescription.MinimumExperienceYears);
            result.EducationMatch = CalculateEducationMatch(resume.Education, jobDescription.EducationRequirement);
            result.LocationMatch = CalculateLocationMatch(resume.Location, jobDescription.Location);

            result.MatchPercentage = CalculateFinalScore(
                result.SkillMatch.Score,
                result.ExperienceMatch.Score,
                result.EducationMatch.Score,
                result.LocationMatch.Score
            );

            return result;
        }

        private SkillMatchDto CalculateSkillMatch(List<string> candidateSkills, List<string> requiredSkills, List<string> preferredSkills)
        {
            var result = new SkillMatchDto();

            var candidateSkillsNormalized = candidateSkills.Select(s => s.ToLower()).ToHashSet();
            var requiredSkillsNormalized = requiredSkills.Select(s => s.ToLower()).ToList();
            var preferredSkillsNormalized = preferredSkills.Select(s => s.ToLower()).ToList();

            result.MatchedRequiredSkills = requiredSkillsNormalized
                .Where(rs => candidateSkillsNormalized.Contains(rs))
                .ToList();

            result.MissingRequiredSkills = requiredSkillsNormalized
                .Where(rs => !candidateSkillsNormalized.Contains(rs))
                .ToList();

            result.MatchedPreferredSkills = preferredSkillsNormalized
                .Where(ps => candidateSkillsNormalized.Contains(ps))
                .ToList();

            result.Score = requiredSkillsNormalized.Count > 0
                ? (double)result.MatchedRequiredSkills.Count / requiredSkillsNormalized.Count
                : 1.0;

            return result;
        }

        private ExperienceMatchDto CalculateExperienceMatch(int? candidateYears, int? requiredYears)
        {
            var result = new ExperienceMatchDto
            {
                CandidateYears = candidateYears,
                RequiredYears = requiredYears
            };

            if (!requiredYears.HasValue)
            {
                result.Score = 1.0;
            }
            else if (!candidateYears.HasValue)
            {
                result.Score = 0.0;
            }
            else if (candidateYears.Value >= requiredYears.Value)
            {
                result.Score = 1.0;
            }
            else
            {
                result.Score = (double)candidateYears.Value / requiredYears.Value;
            }

            return result;
        }

        private EducationMatchDto CalculateEducationMatch(List<EducationDto> candidateEducation, string? requiredEducation)
        {
            var result = new EducationMatchDto();

            if (string.IsNullOrWhiteSpace(requiredEducation))
            {
                result.Score = 1.0;
                result.Matches = true;
            }
            else if (!candidateEducation.Any())
            {
                result.Score = 0.0;
                result.Matches = false;
            }
            else
            {
                result.Matches = candidateEducation.Any(e =>
                    e.Degree?.Contains(requiredEducation, StringComparison.OrdinalIgnoreCase) == true);
                result.Score = result.Matches ? 1.0 : 0.0;
            }

            return result;
        }

        private LocationMatchDto CalculateLocationMatch(string? candidateLocation, string? requiredLocation)
        {
            var result = new LocationMatchDto();

            if (string.IsNullOrWhiteSpace(requiredLocation))
            {
                result.Score = 1.0;
                result.Matches = true;
            }
            else if (string.IsNullOrWhiteSpace(candidateLocation))
            {
                result.Score = 0.0;
                result.Matches = false;
            }
            else
            {
                result.Matches = candidateLocation.Contains(requiredLocation, StringComparison.OrdinalIgnoreCase) ||
                                 requiredLocation.Contains(candidateLocation, StringComparison.OrdinalIgnoreCase);
                result.Score = result.Matches ? 1.0 : 0.0;
            }

            return result;
        }

        private double CalculateFinalScore(double skillScore, double experienceScore, double educationScore, double locationScore)
        {
            return Math.Round((skillScore * 0.5 + experienceScore * 0.2 + educationScore * 0.15 + locationScore * 0.15) * 100, 2);
        }
    }
}
