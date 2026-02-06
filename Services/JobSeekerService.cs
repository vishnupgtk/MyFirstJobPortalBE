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

        public JobSeekerService(DbHelper db)
        {
            _db = db;
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
                    Email = ""
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
                Email = reader["Email"]?.ToString() ?? ""
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
