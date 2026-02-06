namespace AuthSystemApi.DTOs
{
    public class NotificationDto
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "info"; // info, success, warning, error
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public string? RelatedEntityType { get; set; } // "Job", "Application", etc.
        public int? RelatedEntityId { get; set; } // JobId, ApplicationId, etc.
    }
}