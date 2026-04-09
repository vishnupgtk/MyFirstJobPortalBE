namespace AuthSystemApi.Services.Interfaces
{
    public interface IEmailTemplateService
    {
        string GetJobApplicationNotificationSubject(string jobTitle);
        string GetJobApplicationNotificationBody(string jobTitle, string companyName, string jobSeekerName, string jobSeekerEmail, string appliedAt);
        string GetWelcomeEmailSubject();
        string GetWelcomeEmailBody(string userName, string email, string role, string registrationDate);
        string GetPasswordResetSubject();
        string GetPasswordResetBody(string userName, string resetToken, string expiryTime);
    }
}
