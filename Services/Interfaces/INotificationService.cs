using AuthSystemApi.DTOs;

namespace AuthSystemApi.Services.Interfaces
{
    public interface INotificationService
    {
        Task CreateJobApplicationNotificationAsync(int employeeUserId, int jobId, string jobTitle, string applicantName);
        Task CreateApplicationStatusNotificationAsync(int jobSeekerUserId, int jobId, string status);
        Task<List<NotificationDto>> GetUserNotificationsAsync(int userId, bool unreadOnly = false);
        Task MarkNotificationAsReadAsync(int notificationId);
        Task MarkAllNotificationsAsReadAsync(int userId);
        Task<int> GetUnreadNotificationCountAsync(int userId);
    }
}