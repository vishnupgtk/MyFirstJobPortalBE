using AuthSystemApi.Services.EmailTemplate;
using AuthSystemApi.Services.Interfaces;

namespace AuthSystemApi.Services
{
    public class EmailTemplateService : IEmailTemplateService
    {
        public string GetJobApplicationNotificationSubject(string jobTitle)
        {
            return $"New Job Application: {jobTitle}";
        }

        public string GetJobApplicationNotificationBody(string jobTitle, string companyName, string jobSeekerName, string jobSeekerEmail, string appliedAt)
        {
            return Templates.JobApplicationNotification
                .Replace("{{JobTitle}}", jobTitle)
                .Replace("{{CompanyName}}", companyName)
                .Replace("{{JobSeekerName}}", jobSeekerName)
                .Replace("{{JobSeekerEmail}}", jobSeekerEmail)
                .Replace("{{AppliedAt}}", appliedAt);
        }

        public string GetWelcomeEmailSubject()
        {
            return "Welcome to Hiring System";
        }

        public string GetWelcomeEmailBody(string userName, string email, string role, string registrationDate)
        {
            return $"Hello {userName}, your account for {email} as {role} was created on {registrationDate}. Welcome to Hiring System.";
        }

        public string GetPasswordResetSubject()
        {
            return "Password Reset Request";
        }

        public string GetPasswordResetBody(string userName, string resetToken, string expiryTime)
        {
            return $"Hello {userName}, use this reset token: {resetToken}. It expires at {expiryTime}.";
        }
    }
}
