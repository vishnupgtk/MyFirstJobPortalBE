using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AuthSystemApi.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
namespace AuthSystemApi.Services
{
    public class ChatbotService : AuthSystemApi.Services.Interfaces.IChatbotService
    {
        private static readonly ConcurrentDictionary<int, List<ChatMessageDto>> ChatStore = new();
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ChatbotService> _logger;

        public ChatbotService(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<ChatbotService> logger)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<ChatResponseDto> ProcessMessageAsync(int userId, string userType, SendChatMessageDto messageDto)
        {
            var sessionId = string.IsNullOrWhiteSpace(messageDto.SessionId)
                ? Guid.NewGuid().ToString("N")
                : messageDto.SessionId;

            var normalizedMessage = (messageDto.Message ?? string.Empty).Trim();
            var currentPath = (messageDto.CurrentPath ?? string.Empty).Trim().ToLowerInvariant();

            var response =
                await TryGenerateGeminiResponseAsync(userType, normalizedMessage, currentPath)
                ?? BuildResponse(userType, normalizedMessage, currentPath);

            await SaveChatMessageAsync(userId, userType, normalizedMessage, response.Response, sessionId);

            return new ChatResponseDto
            {
                Response = response.Response,
                SessionId = sessionId,
                Source = response.Source,
                SuggestedActions = response.SuggestedActions
            };
        }

        public Task<List<ChatMessageDto>> GetChatHistoryAsync(int userId, string sessionId = null)
        {
            ChatStore.TryGetValue(userId, out var messages);
            messages ??= new List<ChatMessageDto>();

            var history = string.IsNullOrWhiteSpace(sessionId)
                ? messages.OrderBy(m => m.CreatedAt).ToList()
                : messages.Where(m => string.Equals(m.SessionId, sessionId, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(m => m.CreatedAt)
                    .ToList();

            return Task.FromResult(history);
        }

        public Task SaveChatMessageAsync(int userId, string userType, string message, string response, string sessionId)
        {
            var messages = ChatStore.GetOrAdd(userId, _ => new List<ChatMessageDto>());

            lock (messages)
            {
                messages.Add(new ChatMessageDto
                {
                    Id = messages.Count + 1,
                    UserId = userId,
                    UserType = userType,
                    Message = message,
                    Response = response,
                    CreatedAt = DateTime.UtcNow,
                    SessionId = sessionId
                });
            }

            return Task.CompletedTask;
        }

        private static ChatResponseDto BuildResponse(string userType, string message, string currentPath)
        {
            var normalizedRole = (userType ?? string.Empty).Trim();
            var normalizedMessage = (message ?? string.Empty).ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(normalizedMessage))
            {
                return CreateDefaultResponse(normalizedRole, currentPath);
            }

            return normalizedRole switch
            {
                "Employer" => BuildEmployerResponse(normalizedMessage, currentPath),
                "JobSeeker" => BuildJobSeekerResponse(normalizedMessage, currentPath),
                _ => new ChatResponseDto
                {
                    Response = "I can help with the job portal flows for employers and job seekers. Please ask about posting jobs, applications, profiles, or resume tasks.",
                    SuggestedActions = new List<string> { "Go to dashboard", "Open profile" }
                }
            };
        }

        private static ChatResponseDto CreateDefaultResponse(string role, string currentPath)
        {
            if (role == "Employer")
            {
                return new ChatResponseDto
                {
                    Response = currentPath.Contains("/employer/jobs")
                        ? "I can help you manage jobs here. Ask me how to post a job, close a job, review applicants, or improve a job description."
                        : "I can help you as an employer with posting jobs, viewing applicants, updating company profile details, and using your dashboard.",
                    SuggestedActions = new List<string> { "Post a new job", "View my jobs", "Review applicants", "Open company profile" }
                };
            }

            return new ChatResponseDto
            {
                Response = currentPath.Contains("/jobseeker/jobs")
                    ? "I can help you search jobs, understand application status, and improve your profile before applying."
                    : "I can help you as a job seeker with profile updates, resume upload, job search, and tracking applications.",
                SuggestedActions = new List<string> { "Find jobs", "Update profile", "Upload resume", "View my applications" }
            };
        }

        private static ChatResponseDto BuildEmployerResponse(string message, string currentPath)
        {
            if (IsGreeting(message))
            {
                return new ChatResponseDto
                {
                    Response = "Hi, I am here to help with your employer workflow. You can ask me about posting jobs, reviewing applicants, dashboard metrics, or managing your company profile.",
                    SuggestedActions = new List<string> { "Post a new job", "View my jobs", "Review applicants", "Open company profile" }
                };
            }

            if (ContainsAny(message, "help", "what can you do", "assist", "support"))
            {
                return new ChatResponseDto
                {
                    Response = "I can guide you through the employer side of this portal. I can help with creating jobs, checking applicants, understanding dashboard numbers, closing jobs, and updating company details.",
                    SuggestedActions = new List<string> { "Post a new job", "View my jobs", "Review applicants", "Open dashboard" }
                };
            }

            if (ContainsAny(message, "thanks", "thank you", "ok", "okay", "cool"))
            {
                return new ChatResponseDto
                {
                    Response = "You are welcome. If you want, I can also help you with applicants, job posts, or company profile actions.",
                    SuggestedActions = new List<string> { "Review applicants", "View my jobs", "Open dashboard" }
                };
            }

            if (ContainsAny(message, "post job", "create job", "new job", "add job"))
            {
                return new ChatResponseDto
                {
                    Response = "To post a job, open the Post Job page, fill in the title, description, location, employment type, and required skills, then submit. A clear skills list will also help when you review applicants later.",
                    SuggestedActions = new List<string> { "Post a new job", "View my jobs" }
                };
            }

            if (ContainsAny(message, "applicant", "candidate", "application"))
            {
                return new ChatResponseDto
                {
                    Response = "You can review applicants from My Jobs or the dashboard. Open a job's applicants page to sort candidates, check their profiles, and update statuses like Accepted or Rejected.",
                    SuggestedActions = new List<string> { "Review applicants", "View my jobs", "Open dashboard" }
                };
            }

            if (ContainsAny(message, "close job", "stop job", "archive job"))
            {
                return new ChatResponseDto
                {
                    Response = "Go to My Jobs and use the Close Job action for the specific posting. Closed jobs stay visible for reference, but candidates should no longer apply to them.",
                    SuggestedActions = new List<string> { "View my jobs", "Open dashboard" }
                };
            }

            if (ContainsAny(message, "company profile", "profile", "edit company"))
            {
                return new ChatResponseDto
                {
                    Response = "Your company profile can be viewed from My Profile. If editing is enabled, update company information there so job seekers and admins see the latest details.",
                    SuggestedActions = new List<string> { "Open company profile", "Open account" }
                };
            }

            if (ContainsAny(message, "dashboard", "metrics", "overview"))
            {
                return new ChatResponseDto
                {
                    Response = "Your employer dashboard summarizes active jobs, applicants, shortlisted candidates, and recent applications. It is the fastest place to spot which jobs need attention.",
                    SuggestedActions = new List<string> { "Open dashboard", "Review applicants", "View my jobs" }
                };
            }

            if (ContainsAny(message, "where am i", "this page", "help here"))
            {
                return new ChatResponseDto
                {
                    Response = currentPath.Contains("/employer/jobs")
                        ? "You are in the employer job-management flow. From here you can inspect applicants, edit postings, and close jobs."
                        : "You are in the employer area. I can guide you through jobs, applicants, dashboard metrics, and profile management.",
                    SuggestedActions = new List<string> { "View my jobs", "Review applicants", "Open company profile" }
                };
            }

            return BuildGenericEmployerResponse(message, currentPath);
        }

        private static ChatResponseDto BuildJobSeekerResponse(string message, string currentPath)
        {
            if (IsGreeting(message))
            {
                return new ChatResponseDto
                {
                    Response = "Hi, I am here to help with your job seeker workflow. You can ask me about finding jobs, updating your profile, uploading your resume, or checking application status.",
                    SuggestedActions = new List<string> { "Find jobs", "Update profile", "Upload resume", "View my applications" }
                };
            }

            if (ContainsAny(message, "help", "what can you do", "assist", "support"))
            {
                return new ChatResponseDto
                {
                    Response = "I can guide you through the job seeker side of this portal. I can help with job search, application tracking, profile completion, resume upload, and dashboard guidance.",
                    SuggestedActions = new List<string> { "Find jobs", "View my applications", "Update profile", "Open dashboard" }
                };
            }

            if (ContainsAny(message, "thanks", "thank you", "ok", "okay", "cool"))
            {
                return new ChatResponseDto
                {
                    Response = "You are welcome. If you want, I can also help you search jobs, improve your profile, or explain your application status.",
                    SuggestedActions = new List<string> { "Find jobs", "Update profile", "View my applications" }
                };
            }

            if (ContainsAny(message, "find job", "search job", "browse job", "jobs"))
            {
                return new ChatResponseDto
                {
                    Response = "Use the Find Jobs page to browse open roles, then apply directly from the job cards. Before applying, make sure your profile and resume are up to date.",
                    SuggestedActions = new List<string> { "Find jobs", "Update profile", "Upload resume" }
                };
            }

            if (ContainsAny(message, "resume", "cv", "upload resume"))
            {
                return new ChatResponseDto
                {
                    Response = "You can upload your resume from your profile area. Your project already supports uploading and parsing resumes, so keeping that file current helps your profile stay complete.",
                    SuggestedActions = new List<string> { "Upload resume", "Open profile", "View my applications" }
                };
            }

            if (ContainsAny(message, "profile", "complete profile", "update profile"))
            {
                return new ChatResponseDto
                {
                    Response = "Your profile is important for employer visibility. Update your summary, education, college, skills, and resume to improve profile completion before applying.",
                    SuggestedActions = new List<string> { "Update profile", "Upload resume", "Open dashboard" }
                };
            }

            if (ContainsAny(message, "application", "applied", "status"))
            {
                return new ChatResponseDto
                {
                    Response = "Open My Applications to track each application. Pending means it is still under review, Accepted means you moved forward, and Rejected means the employer passed for that role.",
                    SuggestedActions = new List<string> { "View my applications", "Find jobs" }
                };
            }

            if (ContainsAny(message, "dashboard", "stats", "overview"))
            {
                return new ChatResponseDto
                {
                    Response = "Your dashboard shows application counts, available jobs, recent applications, and profile strength. It is a good place to see what to improve next.",
                    SuggestedActions = new List<string> { "Open dashboard", "View my applications", "Update profile" }
                };
            }

            if (ContainsAny(message, "where am i", "this page", "help here"))
            {
                return new ChatResponseDto
                {
                    Response = currentPath.Contains("/jobseeker/jobs")
                        ? "You are on the job search page. You can browse openings, review required skills, and apply directly."
                        : "You are in the job seeker area. I can guide you through profile updates, resume upload, job search, and application tracking.",
                    SuggestedActions = new List<string> { "Find jobs", "View my applications", "Open profile" }
                };
            }

            return BuildGenericJobSeekerResponse(message, currentPath);
        }

        private static bool ContainsAny(string source, params string[] terms)
        {
            return terms.Any(term => source.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsGreeting(string message)
        {
            var normalized = message.Trim().ToLowerInvariant();
            return normalized is "hi" or "hii" or "hiii" or "hello" or "hey" or "helo" or "hola";
        }

        private static ChatResponseDto BuildGenericEmployerResponse(string message, string currentPath)
        {
            var pageHint = currentPath.Contains("/employer/jobs")
                ? "You are currently in the employer job management area."
                : currentPath.Contains("/employer/profile")
                    ? "You are currently in the employer profile area."
                    : "You are currently in the employer section.";

            return new ChatResponseDto
            {
                Response = $"{pageHint} I understood your message as a request for help with \"{message}\". I can guide you on posting jobs, reviewing applicants, checking dashboard insights, closing jobs, or updating company information. Try one of the suggested actions or ask a more specific question.",
                SuggestedActions = new List<string> { "Post a new job", "View my jobs", "Review applicants", "Open dashboard" }
            };
        }

        private static ChatResponseDto BuildGenericJobSeekerResponse(string message, string currentPath)
        {
            var pageHint = currentPath.Contains("/jobseeker/jobs")
                ? "You are currently on the job search page."
                : currentPath.Contains("/jobseeker/profile")
                    ? "You are currently in the profile area."
                    : "You are currently in the job seeker section.";

            return new ChatResponseDto
            {
                Response = $"{pageHint} I understood your message as a request for help with \"{message}\". I can guide you on job search, resume upload, profile completion, application tracking, and dashboard usage. Try one of the suggested actions or ask a more specific question.",
                SuggestedActions = new List<string> { "Find jobs", "Update profile", "Upload resume", "View my applications" }
            };
        }

        private async Task<ChatResponseDto?> TryGenerateGeminiResponseAsync(string userType, string message, string currentPath)
        {
            var isEnabled = _configuration.GetValue<bool>("Gemini:Enabled");
            var apiKey = _configuration["Gemini:ApiKey"];

            if (!isEnabled || string.IsNullOrWhiteSpace(apiKey))
            {
                return null;
            }

            try
            {
                var model = _configuration["Gemini:Model"] ?? "gemini-1.5-flash";
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(15);

                var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                var prompt = BuildGeminiPrompt(userType, message, currentPath);

                var payload = JsonSerializer.Serialize(new
                {
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 250
                    }
                });

                using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var response = await client.PostAsync(endpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Gemini chatbot request failed with status code {StatusCode}. Body: {Body}", response.StatusCode, errorBody);
                    return null;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(responseBody);

                if (!document.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                {
                    return null;
                }

                var parts = candidates[0].GetProperty("content").GetProperty("parts");
                var textParts = new List<string>();

                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out var textElement))
                    {
                        var value = textElement.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            textParts.Add(value.Trim());
                        }
                    }
                }

                var aiText = string.Join(" ", textParts).Trim();
                if (string.IsNullOrWhiteSpace(aiText))
                {
                    return null;
                }

                return new ChatResponseDto
                {
                    Response = aiText,
                    Source = "gemini",
                    SuggestedActions = GetSuggestedActions(userType, currentPath)
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini chatbot fallback triggered");
                return null;
            }
        }

        private static string BuildGeminiPrompt(string userType, string message, string currentPath)
        {
            var roleContext = userType == "Employer"
                ? "The user is an employer using a job portal. Features include dashboard, posting jobs, viewing applicants, closing jobs, and managing company profile."
                : "The user is a job seeker using a job portal. Features include dashboard, finding jobs, uploading resume, updating profile, and tracking applications.";

            return
                $"You are a helpful assistant inside a job portal application. " +
                $"{roleContext} " +
                $"Current page: {currentPath}. " +
                $"User message: {message}. " +
                "Reply naturally in 2-4 sentences, be practical and specific to this portal, do not invent database data, and avoid markdown.";
        }

        private static List<string> GetSuggestedActions(string userType, string currentPath)
        {
            if (userType == "Employer")
            {
                return new List<string> { "Post a new job", "View my jobs", "Review applicants", "Open company profile" };
            }

            return new List<string> { "Find jobs", "Update profile", "Upload resume", "View my applications" };
        }
    }
}
