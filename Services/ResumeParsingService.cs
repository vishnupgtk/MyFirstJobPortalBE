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
            // Try multiple patterns for name extraction
            var patterns = new[]
            {
                @"^([A-Z][a-z]+(?:\s+[A-Z][a-z]*)*)\s*$", // First line name pattern
                @"Name\s*:?\s*([A-Z][a-z]+(?:\s+[A-Z][a-z]*)*)", // "Name:" pattern
                @"^([A-Z][A-Z\s]+)$", // All caps name
                @"([A-Z][a-z]+\s+[A-Z][a-z]+(?:\s+[A-Z][a-z]+)*)" // Standard name pattern
            };

            var lines = text.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();

            // Check first few lines for name
            for (int i = 0; i < Math.Min(3, lines.Count); i++)
            {
                var line = lines[i];

                // Skip lines with email or phone
                if (line.Contains("@") || Regex.IsMatch(line, @"\d{3}[-.\s]?\d{3}[-.\s]?\d{4}"))
                    continue;

                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(line, pattern);
                    if (match.Success && match.Groups[1].Value.Split().Length >= 2)
                    {
                        return match.Groups[1].Value.Trim();
                    }
                }
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
            var patterns = new[]
            {
                @"(\+?\d{1,3}[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}",
                @"(\+?\d{1,3}[-.\s]?)?\d{10}",
                @"Phone\s*:?\s*([+\d\s\-\(\)]+)"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern);
                if (match.Success)
                {
                    return match.Groups[match.Groups.Count - 1].Value.Trim();
                }
            }

            return null;
        }

        private string? ExtractLocation(string text)
        {
            var patterns = new[]
            {
                @"(?:Location|Address|City|Based in)\s*:?\s*([^\n\r]+)",
                @"([A-Z][a-z]+,\s*[A-Z]{2}(?:\s+\d{5})?)", // City, State format
                @"([A-Z][a-z]+\s*,\s*[A-Z][a-z]+)" // City, Country format
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
            }

            return null;
        }

        private List<string> ExtractAndNormalizeSkills(string text)
        {
            var skills = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Enhanced skill patterns with more comprehensive matching
            var skillPatterns = new Dictionary<string, string[]>
            {
                // Programming Languages
                { "JavaScript", new[] { "JavaScript", "JS", "Javascript", "ECMAScript" } },
                { "TypeScript", new[] { "TypeScript", "TS" } },
                { "Python", new[] { "Python", "Py" } },
                { "Java", new[] { "Java" } },
                { "C#", new[] { "C#", "CSharp", "C Sharp", "C-Sharp" } },
                { "C++", new[] { "C++", "CPP", "C Plus Plus" } },
                { "C", new[] { "\\bC\\b" } }, // Word boundary to avoid matching C in other words
                { "PHP", new[] { "PHP" } },
                { "Ruby", new[] { "Ruby" } },
                { "Go", new[] { "\\bGo\\b", "Golang" } },
                { "Rust", new[] { "Rust" } },
                { "Swift", new[] { "Swift" } },
                { "Kotlin", new[] { "Kotlin" } },
                { "Scala", new[] { "Scala" } },
                { "R", new[] { "\\bR\\b" } },
                
                // Frontend Frameworks/Libraries
                { "React", new[] { "React.js", "ReactJS", "React" } },
                { "Angular", new[] { "Angular", "AngularJS", "Angular.js" } },
                { "Vue", new[] { "Vue", "Vue.js", "VueJS" } },
                { "jQuery", new[] { "jQuery", "JQuery" } },
                { "Bootstrap", new[] { "Bootstrap" } },
                { "Tailwind CSS", new[] { "Tailwind", "TailwindCSS", "Tailwind CSS" } },
                
                // Backend Frameworks
                { "Node.js", new[] { "Node.js", "NodeJS", "Node" } },
                { ".NET", new[] { ".NET", "DotNet", "ASP.NET", "ASP.NET Core", ".NET Core" } },
                { "Express", new[] { "Express", "Express.js", "ExpressJS" } },
                { "Django", new[] { "Django" } },
                { "Flask", new[] { "Flask" } },
                { "Spring", new[] { "Spring", "Spring Boot", "SpringBoot" } },
                { "Laravel", new[] { "Laravel" } },
                { "Ruby on Rails", new[] { "Rails", "Ruby on Rails", "RoR" } },
                
                // Databases
                { "SQL", new[] { "SQL", "MySQL", "PostgreSQL", "MS SQL", "SQL Server", "SQLite" } },
                { "MongoDB", new[] { "MongoDB", "Mongo" } },
                { "Redis", new[] { "Redis" } },
                { "Oracle", new[] { "Oracle", "Oracle DB" } },
                { "Cassandra", new[] { "Cassandra" } },
                { "DynamoDB", new[] { "DynamoDB" } },
                
                // Cloud Platforms
                { "AWS", new[] { "AWS", "Amazon Web Services" } },
                { "Azure", new[] { "Azure", "Microsoft Azure" } },
                { "Google Cloud", new[] { "GCP", "Google Cloud", "Google Cloud Platform" } },
                { "Docker", new[] { "Docker" } },
                { "Kubernetes", new[] { "Kubernetes", "K8s" } },
                
                // Tools & Technologies
                { "Git", new[] { "Git", "GitHub", "GitLab", "Bitbucket" } },
                { "Jenkins", new[] { "Jenkins" } },
                { "Jira", new[] { "Jira" } },
                { "Agile", new[] { "Agile", "Scrum", "Kanban" } },
                { "REST API", new[] { "REST", "RESTful", "REST API", "API" } },
                { "GraphQL", new[] { "GraphQL" } },
                { "Microservices", new[] { "Microservices", "Micro-services" } },
                
                // Testing
                { "Unit Testing", new[] { "Unit Testing", "Jest", "JUnit", "NUnit", "PyTest" } },
                { "Selenium", new[] { "Selenium" } },
                
                // Data Science & AI
                { "Machine Learning", new[] { "Machine Learning", "ML", "AI", "Artificial Intelligence" } },
                { "TensorFlow", new[] { "TensorFlow" } },
                { "PyTorch", new[] { "PyTorch" } },
                { "Pandas", new[] { "Pandas" } },
                { "NumPy", new[] { "NumPy", "Numpy" } },
                
                // Mobile Development
                { "React Native", new[] { "React Native", "ReactNative" } },
                { "Flutter", new[] { "Flutter" } },
                { "iOS", new[] { "iOS", "iPhone" } },
                { "Android", new[] { "Android" } }
            };

            // Extract skills from skills section
            var skillsSectionMatch = Regex.Match(text, @"(?:Skills?|Technical Skills?|Core Competencies|Technologies?)\s*:?\s*([^\n]+(?:\n(?!\n)[^\n]+)*)", RegexOptions.IgnoreCase);
            if (skillsSectionMatch.Success)
            {
                var skillsText = skillsSectionMatch.Groups[1].Value;
                ExtractSkillsFromText(skillsText, skillPatterns, skills);
            }

            // Also search throughout the entire resume
            ExtractSkillsFromText(text, skillPatterns, skills);

            return skills.ToList();
        }

        private void ExtractSkillsFromText(string text, Dictionary<string, string[]> skillPatterns, HashSet<string> skills)
        {
            foreach (var skillGroup in skillPatterns)
            {
                foreach (var pattern in skillGroup.Value)
                {
                    if (Regex.IsMatch(text, $@"\b{Regex.Escape(pattern).Replace(@"\\\b", @"\b")}\b", RegexOptions.IgnoreCase))
                    {
                        skills.Add(skillGroup.Key);
                        break; // Found this skill, move to next skill group
                    }
                }
            }
        }

        private List<EducationDto> ExtractEducation(string text)
        {
            var education = new List<EducationDto>();

            // Enhanced education patterns
            var patterns = new[]
            {
                @"(Bachelor(?:'s)?|Master(?:'s)?|PhD|Ph\.D\.|B\.S\.|M\.S\.|B\.A\.|M\.A\.|B\.Tech|M\.Tech|MBA)\s+(?:of|in)?\s*([^,\n]+)(?:,\s*([^,\n]+))?\s*(?:,\s*)?(\d{4})?",
                @"(Bachelor|Master|PhD|Doctorate)\s+(?:of|in)?\s*([^,\n]+)(?:\s*,\s*([^,\n]+))?\s*(?:\((\d{4})\)|\s+(\d{4}))?",
                @"([^,\n]+)\s*,\s*([^,\n]+)\s*,\s*(\d{4})" // Institution, Degree, Year
            };

            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    var edu = new EducationDto();

                    if (match.Groups.Count >= 3)
                    {
                        edu.Degree = match.Groups[1].Value.Trim();
                        edu.Field = match.Groups[2].Value.Trim();

                        if (match.Groups.Count >= 4 && !string.IsNullOrEmpty(match.Groups[3].Value))
                        {
                            if (Regex.IsMatch(match.Groups[3].Value, @"\d{4}"))
                            {
                                edu.Year = match.Groups[3].Value.Trim();
                            }
                            else
                            {
                                edu.Institution = match.Groups[3].Value.Trim();
                            }
                        }

                        if (match.Groups.Count >= 5 && !string.IsNullOrEmpty(match.Groups[4].Value))
                        {
                            edu.Year = match.Groups[4].Value.Trim();
                        }
                    }

                    education.Add(edu);
                }
            }

            return education.Distinct().ToList();
        }

        private List<WorkExperienceDto> ExtractWorkExperience(string text)
        {
            var experience = new List<WorkExperienceDto>();

            // Enhanced work experience patterns
            var patterns = new[]
            {
                @"([A-Z][^,\n]+)\s*(?:at|@)\s*([^,\n]+)\s*(?:,\s*)?(\d{4}|\w+\s+\d{4})\s*[-–]\s*(\d{4}|\w+\s+\d{4}|Present|Current)",
                @"([^,\n]+)\s*,\s*([^,\n]+)\s*(?:\((\d{4})\s*[-–]\s*(\d{4}|Present|Current)\))",
                @"(\d{4}|\w+\s+\d{4})\s*[-–]\s*(\d{4}|\w+\s+\d{4}|Present|Current)\s*:?\s*([^,\n]+)(?:\s*at\s*([^,\n]+))?"
            };

            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    var exp = new WorkExperienceDto();

                    if (match.Groups.Count >= 5)
                    {
                        exp.JobTitle = match.Groups[1].Value.Trim();
                        exp.Company = match.Groups[2].Value.Trim();
                        exp.StartDate = match.Groups[3].Value.Trim();
                        exp.EndDate = match.Groups[4].Value.Trim();
                    }
                    else if (match.Groups.Count >= 4)
                    {
                        exp.StartDate = match.Groups[1].Value.Trim();
                        exp.EndDate = match.Groups[2].Value.Trim();
                        exp.JobTitle = match.Groups[3].Value.Trim();
                        exp.Company = match.Groups.Count > 4 ? match.Groups[4].Value.Trim() : "";
                    }

                    if (!string.IsNullOrEmpty(exp.JobTitle) || !string.IsNullOrEmpty(exp.Company))
                    {
                        experience.Add(exp);
                    }
                }
            }

            return experience;
        }

        private int? CalculateTotalExperience(List<WorkExperienceDto> workExperience)
        {
            if (!workExperience.Any()) return null;

            int totalMonths = 0;
            var currentYear = DateTime.Now.Year;
            var currentMonth = DateTime.Now.Month;

            foreach (var exp in workExperience)
            {
                var startYear = ExtractYear(exp.StartDate);
                var endYear = ExtractYear(exp.EndDate) ?? currentYear;

                if (startYear.HasValue)
                {
                    var startMonth = ExtractMonth(exp.StartDate) ?? 1;
                    var endMonth = ExtractMonth(exp.EndDate) ?? currentMonth;

                    var months = (endYear - startYear.Value) * 12 + (endMonth - startMonth);
                    totalMonths += Math.Max(0, months);
                }
            }

            return totalMonths > 0 ? Math.Max(1, totalMonths / 12) : null;
        }

        private int? ExtractYear(string? dateString)
        {
            if (string.IsNullOrEmpty(dateString)) return null;

            if (dateString.ToLower().Contains("present") || dateString.ToLower().Contains("current"))
                return DateTime.Now.Year;

            var match = Regex.Match(dateString, @"\d{4}");
            return match.Success && int.TryParse(match.Value, out int year) ? year : null;
        }

        private int? ExtractMonth(string? dateString)
        {
            if (string.IsNullOrEmpty(dateString)) return null;

            var monthNames = new Dictionary<string, int>
            {
                {"jan", 1}, {"feb", 2}, {"mar", 3}, {"apr", 4}, {"may", 5}, {"jun", 6},
                {"jul", 7}, {"aug", 8}, {"sep", 9}, {"oct", 10}, {"nov", 11}, {"dec", 12}
            };

            foreach (var month in monthNames)
            {
                if (dateString.ToLower().Contains(month.Key))
                    return month.Value;
            }

            return null;
        }

        private List<string> ExtractCertifications(string text)
        {
            var certifications = new List<string>();

            var patterns = new[]
            {
                @"(?:Certification|Certificate|Certified)\s*:?\s*([^\n]+)",
                @"([A-Z][^,\n]+(?:Certified|Certification|Certificate)[^,\n]*)",
                @"(AWS|Microsoft|Google|Oracle|Cisco|CompTIA)\s+([^,\n]+)"
            };

            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    var cert = match.Groups[match.Groups.Count - 1].Value.Trim();
                    if (!string.IsNullOrEmpty(cert) && cert.Length > 3)
                    {
                        certifications.Add(cert);
                    }
                }
            }

            return certifications.Distinct().ToList();
        }
    }
}
