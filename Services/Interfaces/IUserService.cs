using AuthSystemApi.DTOs;
using AuthSystemApi.Models;

namespace AuthSystemApi.Services.Interfaces
{
    public interface IUserService
    {
        List<User> GetAllUsers();
        PaginatedUsersDto GetUsersPaginated(int pageNumber, int pageSize);
        User GetUserById(int userId);
        void CreateUser(RegisterRequest request);
        void UpdateUser(UpdateUserDto dto);
        void DeleteUser(int userId);
    }
}

