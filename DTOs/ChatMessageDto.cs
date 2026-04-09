using System.ComponentModel.DataAnnotations;

namespace AuthSystemApi.DTOs
{
    public class ChatMessageDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserType { get; set; } // "JobSeeker" or "Employer"
        public string Message { get; set; }
        public string Response { get; set; }
        public DateTime CreatedAt { get; set; }
        public string SessionId { get; set; }
    }

    public class SendChatMessageDto
    {
        [Required]
        public string Message { get; set; } = string.Empty;
        public string? SessionId { get; set; }
        public string? CurrentPath { get; set; }
    }

    public class ChatResponseDto
    {
        public string Response { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string Source { get; set; } = "local";
        public List<string> SuggestedActions { get; set; } = new List<string>();
    }
}
