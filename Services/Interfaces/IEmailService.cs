using AuthSystemApi.DTOs;

namespace AuthSystemApi.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendJobApplicationNotificationAsync(JobApplicationNotificationDto notification);
        Task SendEmailAsync(string to, string subject, string body);
    }
}