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

