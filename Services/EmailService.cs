using AuthSystemApi.DTOs;
using AuthSystemApi.Services.Interfaces;
using AuthSystemApi.Services.EmailTemplate;
using System.Net;
using System.Net.Mail;

namespace AuthSystemApi.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendJobApplicationNotificationAsync(JobApplicationNotificationDto notification)
        {
            var subject = $"New Job Application: {notification.JobTitle}";


            var template = Templates.JobApplicationNotification;


            var body = PopulateJobApplicationTemplate(template, notification);

            await SendEmailAsync(notification.EmployerEmail, subject, body);
        }

        private string PopulateJobApplicationTemplate(
            string template,
            JobApplicationNotificationDto dto)
        {
            return template
                .Replace("{{JobTitle}}", dto.JobTitle)
                .Replace("{{CompanyName}}", dto.CompanyName)
                .Replace("{{JobSeekerName}}", dto.JobSeekerName)
                .Replace("{{JobSeekerEmail}}", dto.JobSeekerEmail)
                .Replace(
                    "{{AppliedAt}}",
                    dto.AppliedAt.ToString("MMM dd, yyyy 'at' hh:mm tt")
                );
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            if (!MailAddress.TryCreate(to, out _))
            {
                _logger.LogWarning($"Invalid recipient email: {to}");
                return;
            }

            try
            {
                var smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var smtpUsername = _configuration["EmailSettings:Username"] ?? "";
                var smtpPassword = _configuration["EmailSettings:Password"] ?? "";
                var fromEmail = _configuration["EmailSettings:FromEmail"] ?? smtpUsername;
                var fromName = _configuration["EmailSettings:FromName"] ?? "Hiring System";

                if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
                {
                    _logger.LogInformation("EMAIL NOTIFICATION (Development Mode)");
                    _logger.LogInformation($"To: {to}");
                    _logger.LogInformation($"Subject: {subject}");
                    _logger.LogInformation($"Body: {body}");
                    return;
                }

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(smtpUsername, smtpPassword)
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                message.To.Add(to);

                await client.SendMailAsync(message);
                _logger.LogInformation($"Email sent successfully to {to}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {to}");
            }
        }

    }
}

