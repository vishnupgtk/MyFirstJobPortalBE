using AuthSystemApi.Data;
using AuthSystemApi.DTOs;
using AuthSystemApi.Services.Interfaces;
using AuthSystemApi.Exceptions;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AuthSystemApi.Services
{
    public class AuthService : IAuthService
    {
        private readonly DbHelper _db;
        private readonly JwtService _jwt;

        public AuthService(DbHelper db, JwtService jwt)
        {
            _db = db;
            _jwt = jwt;
        }

        public void Register(RegisterRequest dto)
        {
            using var con = _db.GetConnection();
            con.Open();

            // Create User
            using var cmd = new SqlCommand("sp_CreateUser", con);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@FirstName", dto.FirstName);
            cmd.Parameters.AddWithValue("@LastName", dto.LastName);
            cmd.Parameters.AddWithValue("@Email", dto.Email);
            cmd.Parameters.AddWithValue(
                "@PasswordHash",
                BCrypt.Net.BCrypt.HashPassword(dto.Password)
            );
            cmd.Parameters.AddWithValue("@RoleId", dto.RoleId);

            cmd.ExecuteNonQuery();

            //  Get newly created UserId
            int userId;
            using (var idCmd = new SqlCommand(
                "SELECT UserId FROM Users WHERE Email = @Email", con))
            {
                idCmd.Parameters.AddWithValue("@Email", dto.Email);
                userId = (int)idCmd.ExecuteScalar();
            }

            //  If JobSeeker → create JobSeekerProfile
            if (dto.RoleId == 3) // JobSeeker
            {
                using var jsCmd = new SqlCommand("sp_CreateJobSeekerProfile", con);
                jsCmd.CommandType = CommandType.StoredProcedure;
                jsCmd.Parameters.AddWithValue("@UserId", userId);
                jsCmd.ExecuteNonQuery();
            }
        }


        public string Login(LoginRequest request)
        {
            using var con = _db.GetConnection();
            using var cmd = new SqlCommand("sp_Login", con);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Email", request.Email);

            con.Open();
            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            var passwordHash = reader["PasswordHash"].ToString();

            if (!BCrypt.Net.BCrypt.Verify(request.Password, passwordHash))
                return null;
            var userId = (int)reader["UserId"];
            var email = reader["Email"].ToString();
            var role = reader["RoleName"].ToString();
            var firstName = reader["FirstName"].ToString();
            var lastName = reader["LastName"].ToString();


            return _jwt.GenerateToken(userId,email, role,firstName,lastName);
        }

        public bool ResetPassword(string email, string newPassword)
        {
            using var con = _db.GetConnection();
            using var cmd = new SqlCommand("sp_ResetPassword", con);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue(
                "@PasswordHash",
                BCrypt.Net.BCrypt.HashPassword(newPassword)
            );

            con.Open();
            int rowsAffected = cmd.ExecuteNonQuery();

            return rowsAffected > 0;
        }
    }
}
