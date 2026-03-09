using AuthSystemApi.DTOs;
using System.Text.RegularExpressions;

namespace AuthSystemApi.Services
{
    public class JobDescriptionParsingService
    {
        public JobDescriptionParseResponseDto ParseJobDescription(string jobDescriptionText)
        {
            var result = new JobDescriptionParseResponseDto();

            result.JobTitle = ExtractJobTitle(jobDescriptionText);
            result.RequiredSkills = ExtractSkills(jobDescriptionText, "required");
            result.PreferredSkills = ExtractSkills(jobDescriptionText, "preferred");
            result.MinimumExperienceYears = ExtractExperienceYears(jobDescriptionText);
            result.EducationRequirement = ExtractEducationRequirement(jobDescriptionText);
            result.Location = ExtractLocation(jobDescriptionText);
            result.Industry = ExtractIndustry(jobDescriptionText);
            result.SeniorityLevel = ExtractSeniorityLevel(jobDescriptionText);

            return result;
        }

        private string? ExtractJobTitle(string text)
        {
            var match = Regex.Match(text, @"(Job Title|Position|Role):\s*([^\n]+)", RegexOptions.IgnoreCase);
            if (match.Success) return match.Groups[2].Value.Trim();

            var lines = text.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            return lines.FirstOrDefault();
        }

        private List<string> ExtractSkills(string text, string type)
        {
            var skills = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pattern = type == "required"
                ? @"(Required Skills|Requirements|Must Have):\s*([^\n]+(?:\n(?!\n)[^\n]+)*)"
                : @"(Preferred Skills|Nice to Have|Bonus):\s*([^\n]+(?:\n(?!\n)[^\n]+)*)";

            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var skillText = match.Groups[2].Value;
                var skillMatches = Regex.Matches(skillText, @"\b[A-Z][A-Za-z0-9.#+]*\b");
                foreach (Match skillMatch in skillMatches)
                {
                    skills.Add(NormalizeSkill(skillMatch.Value));
                }
            }

            return skills.ToList();
        }

        private string NormalizeSkill(string skill)
        {
            var normalizations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "React.js", "React" },
                { "ReactJS", "React" },
                { "Node.js", "Node.js" },
                { "NodeJS", "Node.js" },
                { "MS SQL", "SQL" },
                { "MySQL", "SQL" },
                { "PostgreSQL", "SQL" }
            };

            return normalizations.TryGetValue(skill, out var normalized) ? normalized : skill;
        }

        private int? ExtractExperienceYears(string text)
        {
            var match = Regex.Match(text, @"(\d+)\+?\s*(years?|yrs?)\s*(of\s*)?(experience|exp)", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int years))
            {
                return years;
            }
            return null;
        }

        private string? ExtractEducationRequirement(string text)
        {
            var match = Regex.Match(text, @"(Education|Degree):\s*([^\n]+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[2].Value.Trim() : null;
        }

        private string? ExtractLocation(string text)
        {
            var match = Regex.Match(text, @"(Location|City|Office):\s*([^\n]+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[2].Value.Trim() : null;
        }

        private string? ExtractIndustry(string text)
        {
            var match = Regex.Match(text, @"(Industry|Sector):\s*([^\n]+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[2].Value.Trim() : null;
        }

        private string? ExtractSeniorityLevel(string text)
        {
            var levels = new[] { "Junior", "Mid-Level", "Senior", "Lead", "Principal", "Entry" };
            foreach (var level in levels)
            {
                if (text.Contains(level, StringComparison.OrdinalIgnoreCase))
                    return level;
            }
            return null;
        }
    }
}
