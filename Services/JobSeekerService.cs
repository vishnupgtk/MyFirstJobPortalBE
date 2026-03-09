using AuthSystemApi.Data;
using AuthSystemApi.DTOs;
using AuthSystemApi.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AuthSystemApi.Services
{
    public class JobSeekerService : IJobSeekerService
    {
        private readonly DbHelper _db;
        private readonly IWebHostEnvironment _env;

        public JobSeekerService(DbHelper db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }


        // PROFILE (VIEW)


        public JobSeekerProfileViewDto GetProfile(int userId)
        {
            using var con = _db.GetConnection();
            using var cmd = new SqlCommand("sp_GetJobSeekerProfile", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserId", userId);

            con.Open();
            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
            {
                // Profile row exists but no data yet (safe fallback)
                return new JobSeekerProfileViewDto
                {
                    UserId = userId,
                    JobSeekerId = 0,
                    Summary = "",
                    Education = "",
                    College = "",
                    Skills = "",
                    FullName = "",
                    Email = "",
                    ResumeFileName = null,
                    ResumeFilePath = null,
                    ResumeUploadedAt = null
                };
            }

            return new JobSeekerProfileViewDto
            {
                JobSeekerId = reader["JobSeekerId"] != DBNull.Value
                                ? Convert.ToInt32(reader["JobSeekerId"])
                                : 0,
                UserId = Convert.ToInt32(reader["UserId"]),
                Summary = reader["Summary"]?.ToString() ?? "",
                Education = reader["Education"]?.ToString() ?? "",
                College = reader["College"]?.ToString() ?? "",
                Skills = reader["Skills"]?.ToString() ?? "",
                FullName = $"{reader["FirstName"]} {reader["LastName"]}",
                Email = reader["Email"]?.ToString() ?? "",
                ResumeFileName = reader["ResumeFileName"] != DBNull.Value ? reader["ResumeFileName"].ToString() : null,
                ResumeFilePath = reader["ResumeFilePath"] != DBNull.Value ? reader["ResumeFilePath"].ToString() : null,
                ResumeUploadedAt = reader["ResumeUploadedAt"] != DBNull.Value ? Convert.ToDateTime(reader["ResumeUploadedAt"]) : null
            };
        }


        // PROFILE (UPDATE)


        public void UpdateProfile(JobSeekerProfileUpdateDto dto)
        {
            using var con = _db.GetConnection();
            using var cmd = new SqlCommand("sp_UpdateJobSeekerProfile", con);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@UserId", dto.UserId);
            cmd.Parameters.AddWithValue("@Summary", dto.Summary ?? "");
            cmd.Parameters.AddWithValue("@Education", dto.Education ?? "");
            cmd.Parameters.AddWithValue("@College", dto.College ?? "");
            cmd.Parameters.AddWithValue("@Skills", dto.Skills ?? "");

            con.Open();
            cmd.ExecuteNonQuery(); // safe because profile exists
        }


        // RESUME UPLOAD


        public async Task<string> UploadResume(int userId, IFormFile file)
        {
            // Validate file
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file uploaded");

            var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                throw new ArgumentException("Only PDF, DOC, and DOCX files are allowed");

            if (file.Length > 5 * 1024 * 1024) // 5MB limit
                throw new ArgumentException("File size must be less than 5MB");

            // Create uploads directory if it doesn't exist
            var uploadsPath = Path.Combine(_env.ContentRootPath, "Uploads", "Resumes");
            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            // Delete old resume if exists
            await DeleteOldResume(userId);

            // Generate unique filename
            var fileName = $"{userId}_{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Update database
            using var con = _db.GetConnection();
            using var cmd = new SqlCommand(@"
                UPDATE JobSeekerProfiles 
                SET ResumeFileName = @FileName, 
                    ResumeFilePath = @FilePath,
                    ResumeUploadedAt = GETDATE()
                WHERE UserId = @UserId", con);

            cmd.Parameters.AddWithValue("@FileName", file.FileName);
            cmd.Parameters.AddWithValue("@FilePath", fileName);
            cmd.Parameters.AddWithValue("@UserId", userId);

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return fileName;
        }


        // DELETE RESUME


        public async Task DeleteResume(int userId)
        {
            await DeleteOldResume(userId);

            // Clear database fields
            using var con = _db.GetConnection();
            using var cmd = new SqlCommand(@"
                UPDATE JobSeekerProfiles 
                SET ResumeFileName = NULL, 
                    ResumeFilePath = NULL,
                    ResumeUploadedAt = NULL
                WHERE UserId = @UserId", con);

            cmd.Parameters.AddWithValue("@UserId", userId);

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task DeleteOldResume(int userId)
        {
            // Get old resume path
            using var con = _db.GetConnection();
            using var cmd = new SqlCommand(@"
                SELECT ResumeFilePath 
                FROM JobSeekerProfiles 
                WHERE UserId = @UserId", con);

            cmd.Parameters.AddWithValue("@UserId", userId);

            await con.OpenAsync();
            var oldFilePath = await cmd.ExecuteScalarAsync() as string;

            if (!string.IsNullOrEmpty(oldFilePath))
            {
                var fullPath = Path.Combine(_env.ContentRootPath, "Uploads", "Resumes", oldFilePath);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
        }


        // JOBSEEKER → OWN HISTORY


        public async Task<List<JobSeekerChangeHistoryDto>> GetHistory(int userId)
        {
            var list = new List<JobSeekerChangeHistoryDto>();

            using var con = _db.GetConnection();
            using var cmd = new SqlCommand(@"
                SELECT 
                    h.FieldName,
                    h.OldValue,
                    h.NewValue,
                    u.FirstName + ' ' + u.LastName AS ChangedBy,
                    h.ChangedAt
                FROM JobSeekerProfileChangeHistory h
                JOIN JobSeekerProfiles js ON h.JobSeekerId = js.JobSeekerId
                JOIN Users u ON h.ChangedBy = u.UserId
                WHERE js.UserId = @UserId
                ORDER BY h.ChangedAt DESC", con);

            cmd.Parameters.AddWithValue("@UserId", userId);

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new JobSeekerChangeHistoryDto
                {
                    FieldName = reader["FieldName"].ToString(),
                    OldValue = reader["OldValue"]?.ToString(),
                    NewValue = reader["NewValue"]?.ToString(),
                    ChangedBy = reader["ChangedBy"].ToString(),
                    ChangedAt = Convert.ToDateTime(reader["ChangedAt"])
                });
            }

            return list;
        }


        // ADMIN → ALL JOBSEEKER HISTORY

        public async Task<List<JobSeekerChangeHistoryDto>> GetAllHistory()
        {
            var list = new List<JobSeekerChangeHistoryDto>();

            using var con = _db.GetConnection();
            using var cmd = new SqlCommand(@"
                SELECT 
                    u.FirstName + ' ' + u.LastName AS ChangedBy,
                    h.FieldName,
                    h.OldValue,
                    h.NewValue,
                    h.ChangedAt
                FROM JobSeekerProfileChangeHistory h
                JOIN Users u ON h.ChangedBy = u.UserId
                ORDER BY h.ChangedAt DESC", con);

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new JobSeekerChangeHistoryDto
                {
                    FieldName = reader["FieldName"].ToString(),
                    OldValue = reader["OldValue"]?.ToString(),
                    NewValue = reader["NewValue"]?.ToString(),
                    ChangedBy = reader["ChangedBy"].ToString(),
                    ChangedAt = Convert.ToDateTime(reader["ChangedAt"])
                });
            }

            return list;
        }
    }
}
