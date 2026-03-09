using AuthSystemApi.DTOs;
using System.Text.RegularExpressions;

namespace AuthSystemApi.Services
{
    public class ResumeParsingService
    {
        public ResumeParseResponseDto ParseResume(string resumeText)
        {
            var result = new ResumeParseResponseDto();

            result.FullName = ExtractFullName(resumeText);
            result.Email = ExtractEmail(resumeText);
            result.Phone = ExtractPhone(resumeText);
            result.Location = ExtractLocation(resumeText);
            result.Skills = ExtractAndNormalizeSkills(resumeText);
            result.Education = ExtractEducation(resumeText);
            result.WorkExperience = ExtractWorkExperience(resumeText);
            result.TotalExperienceYears = CalculateTotalExperience(result.WorkExperience);
            result.CurrentJobTitle = result.WorkExperience.FirstOrDefault()?.JobTitle;
            result.Certifications = ExtractCertifications(resumeText);

            return result;
        }

        private string? ExtractFullName(string text)
        {
            var lines = text.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            if (lines.Count > 0)
            {
                var firstLine = lines[0];
                if (Regex.IsMatch(firstLine, @"^[A-Z][a-z]+(\s[A-Z][a-z]+)+$"))
                    return firstLine;
            }
            return null;
        }

        private string? ExtractEmail(string text)
        {
            var match = Regex.Match(text, @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b");
            return match.Success ? match.Value : null;
        }

        private string? ExtractPhone(string text)
        {
            var match = Regex.Match(text, @"(\+?\d{1,3}[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}");
            return match.Success ? match.Value : null;
        }

        private string? ExtractLocation(string text)
        {
            var match = Regex.Match(text, @"(Location|Address|City):\s*([^\n]+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[2].Value.Trim() : null;
        }

        private List<string> ExtractAndNormalizeSkills(string text)
        {
            var skills = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var skillPatterns = new Dictionary<string, string[]>
            {
                { "React", new[] { "React.js", "ReactJS", "React" } },
                { "Node.js", new[] { "Node.js", "NodeJS", "Node" } },
                { "JavaScript", new[] { "JavaScript", "JS" } },
                { "TypeScript", new[] { "TypeScript", "TS" } },
                { "SQL", new[] { "SQL", "MS SQL", "MySQL", "PostgreSQL" } },
                { "C#", new[] { "C#", "CSharp", "C Sharp" } },
                { ".NET", new[] { ".NET", "DotNet", "ASP.NET" } },
                { "Python", new[] { "Python", "Py" } },
                { "Java", new[] { "Java" } },
                { "Angular", new[] { "Angular", "AngularJS" } },
                { "Vue", new[] { "Vue", "Vue.js", "VueJS" } }
            };

            foreach (var pattern in skillPatterns)
            {
                foreach (var variant in pattern.Value)
                {
                    if (Regex.IsMatch(text, $@"\b{Regex.Escape(variant)}\b", RegexOptions.IgnoreCase))
                    {
                        skills.Add(pattern.Key);
                        break;
                    }
                }
            }

            return skills.ToList();
        }

        private List<EducationDto> ExtractEducation(string text)
        {
            var education = new List<EducationDto>();
            var degreePattern = @"(Bachelor|Master|PhD|B\.S\.|M\.S\.|B\.A\.|M\.A\.).*?(\d{4})";
            var matches = Regex.Matches(text, degreePattern, RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                education.Add(new EducationDto
                {
                    Degree = match.Groups[1].Value,
                    Year = match.Groups[2].Value
                });
            }

            return education;
        }

        private List<WorkExperienceDto> ExtractWorkExperience(string text)
        {
            var experience = new List<WorkExperienceDto>();
            var datePattern = @"(\d{4}|\w+\s+\d{4})\s*[-–]\s*(\d{4}|\w+\s+\d{4}|Present|Current)";
            var matches = Regex.Matches(text, datePattern, RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                experience.Add(new WorkExperienceDto
                {
                    StartDate = match.Groups[1].Value,
                    EndDate = match.Groups[2].Value
                });
            }

            return experience;
        }

        private int? CalculateTotalExperience(List<WorkExperienceDto> workExperience)
        {
            if (!workExperience.Any()) return null;

            int totalYears = 0;
            foreach (var exp in workExperience)
            {
                if (int.TryParse(Regex.Match(exp.StartDate ?? "", @"\d{4}").Value, out int startYear))
                {
                    var endYearStr = exp.EndDate?.ToLower().Contains("present") == true ||
                                     exp.EndDate?.ToLower().Contains("current") == true
                        ? DateTime.Now.Year.ToString()
                        : Regex.Match(exp.EndDate ?? "", @"\d{4}").Value;

                    if (int.TryParse(endYearStr, out int endYear))
                    {
                        totalYears += Math.Max(0, endYear - startYear);
                    }
                }
            }

            return totalYears > 0 ? totalYears : null;
        }

        private List<string> ExtractCertifications(string text)
        {
            var certifications = new List<string>();
            var certPattern = @"(Certified|Certification):\s*([^\n]+)";
            var matches = Regex.Matches(text, certPattern, RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                certifications.Add(match.Groups[2].Value.Trim());
            }

            return certifications;
        }
    }
}
