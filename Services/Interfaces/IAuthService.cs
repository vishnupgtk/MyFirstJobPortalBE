using AuthSystemApi.DTOs;

namespace AuthSystemApi.Services.Interfaces
{
    public interface IAuthService
    {
        void Register(RegisterRequest request);  
        string Login(LoginRequest request);

        bool ResetPassword(string email, string newPassword);
    }
}
