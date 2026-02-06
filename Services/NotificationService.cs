using AuthSystemApi.Data;
using AuthSystemApi.DTOs;
using AuthSystemApi.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AuthSystemApi.Services
{
    public class NotificationService : INotificationService
    {
        private readonly DbHelper _db;

        public NotificationService(DbHelper db)
        {
            _db = db;
        }

        public async Task CreateJobApplicationNotificationAsync(int employeeUserId, int jobId, string jobTitle, string applicantName)
        {
            try
            {
                Console.WriteLine($"DEBUG: Creating notification for employee {employeeUserId}, job '{jobTitle}', applicant '{applicantName}'");

                using var con = _db.GetConnection();
                using var cmd = new SqlCommand(@"
                    INSERT INTO Notifications (UserId, Title, Message, Type, RelatedEntityType, RelatedEntityId)
                    VALUES (@UserId, @Title, @Message, @Type, @RelatedEntityType, @RelatedEntityId)", con);

                cmd.Parameters.AddWithValue("@UserId", employeeUserId);
                cmd.Parameters.AddWithValue("@Title", "New Job Application");
                cmd.Parameters.AddWithValue("@Message", $"{applicantName} applied for '{jobTitle}'");
                cmd.Parameters.AddWithValue("@Type", "info");
                cmd.Parameters.AddWithValue("@RelatedEntityType", "Job");
                cmd.Parameters.AddWithValue("@RelatedEntityId", jobId);

                await con.OpenAsync();
                var rowsAffected = await cmd.ExecuteNonQueryAsync();
                Console.WriteLine($"DEBUG: Notification inserted successfully, rows affected: {rowsAffected}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Failed to create notification: {ex.Message}");
                Console.WriteLine($"DEBUG: Stack trace: {ex.StackTrace}");
                throw; // Re-throw to see the error in the main flow
            }
        }

        public async Task<List<NotificationDto>> GetUserNotificationsAsync(int userId, bool unreadOnly = false)
        {
            var notifications = new List<NotificationDto>();

            using var con = _db.GetConnection();
            var sql = @"
                SELECT NotificationId, UserId, Title, Message, Type, IsRead, CreatedAt, RelatedEntityType, RelatedEntityId
                FROM Notifications 
                WHERE UserId = @UserId";

            if (unreadOnly)
                sql += " AND IsRead = 0";

            sql += " ORDER BY CreatedAt DESC";

            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@UserId", userId);

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                notifications.Add(new NotificationDto
                {
                    NotificationId = (int)reader["NotificationId"],
                    UserId = (int)reader["UserId"],
                    Title = reader["Title"].ToString() ?? "",
                    Message = reader["Message"].ToString() ?? "",
                    Type = reader["Type"].ToString() ?? "info",
                    IsRead = (bool)reader["IsRead"],
                    CreatedAt = (DateTime)reader["CreatedAt"],
                    RelatedEntityType = reader["RelatedEntityType"]?.ToString(),
                    RelatedEntityId = reader["RelatedEntityId"] as int?
                });
            }

            return notifications;
        }

        public async Task MarkNotificationAsReadAsync(int notificationId)
        {
            using var con = _db.GetConnection();
            using var cmd = new SqlCommand("UPDATE Notifications SET IsRead = 1 WHERE NotificationId = @NotificationId", con);
            cmd.Parameters.AddWithValue("@NotificationId", notificationId);

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task MarkAllNotificationsAsReadAsync(int userId)
        {
            using var con = _db.GetConnection();
            using var cmd = new SqlCommand("UPDATE Notifications SET IsRead = 1 WHERE UserId = @UserId", con);
            cmd.Parameters.AddWithValue("@UserId", userId);

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> GetUnreadNotificationCountAsync(int userId)
        {
            using var con = _db.GetConnection();
            using var cmd = new SqlCommand("SELECT COUNT(*) FROM Notifications WHERE UserId = @UserId AND IsRead = 0", con);
            cmd.Parameters.AddWithValue("@UserId", userId);

            await con.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task CreateApplicationStatusNotificationAsync(int jobSeekerUserId, int jobId, string status)
        {
            try
            {
                // Get job details
                using var con = _db.GetConnection();
                using var cmd = new SqlCommand(@"
                    SELECT Title, PostedByUserId 
                    FROM Jobs 
                    WHERE JobId = @JobId", con);

                cmd.Parameters.AddWithValue("@JobId", jobId);

                await con.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var jobTitle = reader["Title"].ToString();

                    var message = status.ToLower() switch
                    {
                        "accepted" => $"Great news! Your application for '{jobTitle}' has been accepted.",
                        "rejected" => $"Your application for '{jobTitle}' has been reviewed.",
                        "reviewed" => $"Your application for '{jobTitle}' is being reviewed.",
                        _ => $"Your application status for '{jobTitle}' has been updated to {status}."
                    };

                    reader.Close();

                    // Create notification
                    using var insertCmd = new SqlCommand(@"
                        INSERT INTO Notifications (UserId, Title, Message, Type, CreatedAt, IsRead)
                        VALUES (@UserId, @Title, @Message, @Type, GETDATE(), 0)", con);

                    insertCmd.Parameters.AddWithValue("@UserId", jobSeekerUserId);
                    insertCmd.Parameters.AddWithValue("@Title", "Application Status Update");
                    insertCmd.Parameters.AddWithValue("@Message", message);
                    insertCmd.Parameters.AddWithValue("@Type", "ApplicationStatus");

                    await insertCmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create application status notification: {ex.Message}");
                throw;
            }
        }
    }
}