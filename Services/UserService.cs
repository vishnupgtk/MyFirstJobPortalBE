using AuthSystemApi.Data;
using AuthSystemApi.Services.Interfaces;
using AuthSystemApi.DTOs;
using AuthSystemApi.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AuthSystemApi.Services;

public class UserService : IUserService
{
    private readonly DbHelper _db;

    public UserService(DbHelper db)
    {
        _db = db;
    }

    // GET ALL
    public List<User> GetAllUsers()
    {
        var users = new List<User>();

        using var con = _db.GetConnection();
        using var cmd = new SqlCommand("sp_GetAllUsers", con);
        cmd.CommandType = CommandType.StoredProcedure;

        con.Open();
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            users.Add(MapUser(reader));
        }

        return users;
    }

    // GET PAGINATED
    public PaginatedUsersDto GetUsersPaginated(int pageNumber, int pageSize)
    {
        var result = new PaginatedUsersDto
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Users = new List<User>()
        };

        using var con = _db.GetConnection();

        // Get total count
        using var countCmd = new SqlCommand("sp_GetUsersCount", con);
        countCmd.CommandType = CommandType.StoredProcedure;
        con.Open();
        result.TotalCount = (int)countCmd.ExecuteScalar();
        result.TotalPages = (int)Math.Ceiling((double)result.TotalCount / pageSize);
        result.HasPreviousPage = pageNumber > 1;
        result.HasNextPage = pageNumber < result.TotalPages;

        // Get paginated users
        using var cmd = new SqlCommand("sp_GetUsersPaginated", con);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
        cmd.Parameters.AddWithValue("@PageSize", pageSize);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Users.Add(MapUser(reader));
        }

        return result;
    }

    // GET BY ID
    public User GetUserById(int userId)
    {
        using var con = _db.GetConnection();
        using var cmd = new SqlCommand("sp_GetUserById", con);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@UserId", userId);

        con.Open();
        using var reader = cmd.ExecuteReader();

        return reader.Read() ? MapUser(reader) : null;
    }

    // POST
    public void CreateUser(RegisterRequest req)
    {
        using var con = _db.GetConnection();
        using var cmd = new SqlCommand("sp_CreateUser", con);
        cmd.CommandType = CommandType.StoredProcedure;

        cmd.Parameters.AddWithValue("@FirstName", req.FirstName);
        cmd.Parameters.AddWithValue("@LastName", req.LastName);
        cmd.Parameters.AddWithValue("@Email", req.Email);
        cmd.Parameters.AddWithValue("@PasswordHash",
            BCrypt.Net.BCrypt.HashPassword(req.Password));
        cmd.Parameters.AddWithValue("@RoleId", req.RoleId);

        con.Open();
        cmd.ExecuteNonQuery();
    }

    // PUT
    public void UpdateUser(UpdateUserDto dto)
    {
        using var con = _db.GetConnection();
        using var cmd = new SqlCommand("sp_UpdateUserName", con);
        cmd.CommandType = CommandType.StoredProcedure;

        cmd.Parameters.AddWithValue("@UserId", dto.UserId);
        cmd.Parameters.AddWithValue("@FirstName", dto.FirstName);
        cmd.Parameters.AddWithValue("@LastName", dto.LastName);

        con.Open();
        cmd.ExecuteNonQuery();
    }

    // DELETE
    public void DeleteUser(int userId)
    {
        using var con = _db.GetConnection();
        using var cmd = new SqlCommand("sp_DeleteUser", con);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@UserId", userId);

        con.Open();
        cmd.ExecuteNonQuery();
    }

    // GET ADMIN STATISTICS
    public AdminStatisticsDto GetAdminStatistics()
    {
        var stats = new AdminStatisticsDto();

        using var con = _db.GetConnection();
        con.Open();

        // Get total users (excluding admin)
        using (var cmd = new SqlCommand(@"
            SELECT COUNT(*) FROM Users u
            INNER JOIN Roles r ON u.RoleId = r.RoleId
            WHERE r.RoleName != 'Admin'", con))
        {
            stats.TotalUsers = (int)cmd.ExecuteScalar();
        }

        // Get active employers
        using (var cmd = new SqlCommand(@"
            SELECT COUNT(*) FROM Users u
            INNER JOIN Roles r ON u.RoleId = r.RoleId
            WHERE r.RoleName = 'Employer'", con))
        {
            stats.ActiveEmployers = (int)cmd.ExecuteScalar();
        }

        // Get job seekers
        using (var cmd = new SqlCommand(@"
            SELECT COUNT(*) FROM Users u
            INNER JOIN Roles r ON u.RoleId = r.RoleId
            WHERE r.RoleName = 'JobSeeker'", con))
        {
            stats.JobSeekers = (int)cmd.ExecuteScalar();
        }

        // Get active jobs
        using (var cmd = new SqlCommand(@"
            SELECT COUNT(*) FROM Jobs
            WHERE Status = 'Open' AND IsDeleted = 0", con))
        {
            stats.ActiveJobs = (int)cmd.ExecuteScalar();
        }

        // Calculate percentage changes (comparing to last week)
        // For now, using simple random percentages between -5 and +15
        // You can implement real calculations based on historical data
        stats.UsersChangePercent = CalculateGrowthPercentage(stats.TotalUsers);
        stats.EmployersChangePercent = CalculateGrowthPercentage(stats.ActiveEmployers);
        stats.JobSeekersChangePercent = CalculateGrowthPercentage(stats.JobSeekers);
        stats.JobsChangePercent = CalculateGrowthPercentage(stats.ActiveJobs);

        return stats;
    }

    private decimal CalculateGrowthPercentage(int currentCount)
    {
        // Simple growth calculation: if count > 10, show positive growth
        // In real app, you'd compare with historical data
        if (currentCount == 0) return 0;
        if (currentCount < 5) return new Random().Next(-5, 3);
        if (currentCount < 20) return new Random().Next(0, 8);
        return new Random().Next(3, 15);
    }

    private User MapUser(SqlDataReader reader)
    {
        return new User
        {
            UserId = (int)reader["UserId"],
            FirstName = reader["FirstName"].ToString(),
            LastName = reader["LastName"].ToString(),
            Email = reader["Email"].ToString(),
            RoleName = reader["RoleName"].ToString()
        };
    }
}

