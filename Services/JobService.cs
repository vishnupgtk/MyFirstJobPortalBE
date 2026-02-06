using AuthSystemApi.Data;
using AuthSystemApi.DTOs;
using AuthSystemApi.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AuthSystemApi.Services
{
    public class JobService : IJobService
    {
        private readonly DbHelper _db;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;

        public JobService(DbHelper db, IEmailService emailService, INotificationService notificationService)
        {
            _db = db;
            _emailService = emailService;
            _notificationService = notificationService;
        }

        // EMPLOYER → CREATE JOB + LOG ACTIVITY

        public void CreateJob(int userId, CreateJobDto dto)
        {
            using var con = _db.GetConnection();
            con.Open();

            using var tran = con.BeginTransaction();

            try
            {
                int jobId;

                using (var cmd = new SqlCommand("sp_CreateJob", con, tran))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@PostedByUserId", userId);
                    cmd.Parameters.AddWithValue("@Title", dto.Title);
                    cmd.Parameters.AddWithValue("@Description", dto.Description);
                    cmd.Parameters.AddWithValue("@RequiredSkills", dto.RequiredSkills ?? "");
                    cmd.Parameters.AddWithValue("@ExperienceLevel", dto.ExperienceLevel ?? "");
                    cmd.Parameters.AddWithValue("@EmploymentType", dto.EmploymentType ?? "");
                    cmd.Parameters.AddWithValue("@Location", dto.Location ?? "");
                    cmd.Parameters.AddWithValue("@SalaryRange", dto.SalaryRange ?? "");

                    // OUTPUT JobId (recommended professional approach)
                    var outParam = new SqlParameter("@JobId", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outParam);

                    cmd.ExecuteNonQuery();
                    jobId = (int)outParam.Value;
                }

                // LOG ACTIVITY
                using (var logCmd = new SqlCommand("sp_LogJobActivity", con, tran))
                {
                    logCmd.CommandType = CommandType.StoredProcedure;
                    logCmd.Parameters.AddWithValue("@JobId", jobId);
                    logCmd.Parameters.AddWithValue("@Action", "Created");
                    logCmd.Parameters.AddWithValue("@PerformedBy", userId);
                    logCmd.ExecuteNonQuery();
                }

                tran.Commit();
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        // EMPLOYER → VIEW OPEN JOBS

        public async Task<List<JobListDto>> GetOpenJobs()
        {
            var list = new List<JobListDto>();

            using var con = _db.GetConnection();
            using var cmd = new SqlCommand("sp_GetOpenJobs", con);
            cmd.CommandType = CommandType.StoredProcedure;

            await con.OpenAsync();
            using var rd = await cmd.ExecuteReaderAsync();

            while (await rd.ReadAsync())
            {
                list.Add(new JobListDto
                {
                    JobId = (int)rd["JobId"],
                    Title = rd["Title"].ToString(),
                    Description = rd["Description"].ToString(),
                    RequiredSkills = rd["RequiredSkills"]?.ToString(),
                    Location = rd["Location"]?.ToString(),
                    EmploymentType = rd["EmploymentType"]?.ToString(),
                    PostedBy = rd["PostedBy"].ToString(),
                    CreatedAt = (DateTime)rd["CreatedAt"]
                });
            }

            return list;
        }

        // EMPLOYER → VIEW MY JOBS

        public async Task<List<JobListDto>> GetMyJobs(int userId)
        {
            var list = new List<JobListDto>();

            using var con = _db.GetConnection();
            using var cmd = new SqlCommand("sp_GetMyJobs", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserId", userId);

            await con.OpenAsync();
            using var rd = await cmd.ExecuteReaderAsync();

            while (await rd.ReadAsync())
            {
                list.Add(new JobListDto
                {
                    JobId = (int)rd["JobId"],
                    Title = rd["Title"].ToString(),
                    Description = rd["Description"].ToString(),
                    RequiredSkills = rd["RequiredSkills"]?.ToString(),
                    Location = rd["Location"]?.ToString(),
                    EmploymentType = rd["EmploymentType"]?.ToString(),
                    PostedBy = rd["PostedBy"].ToString(),
                    CreatedAt = (DateTime)rd["CreatedAt"],
                    ApplicantCount = rd["ApplicantCount"] != DBNull.Value ? (int)rd["ApplicantCount"] : 0
                });
            }

            return list;
        }

        // JOBSEEKER → APPLY FOR JOB + LOG ACTIVITY + SEND EMAIL NOTIFICATION

        public async void ApplyForJob(int jobId, int jobSeekerUserId)
        {
            using var con = _db.GetConnection();
            con.Open();

            using var tran = con.BeginTransaction();

            try
            {
                // Apply for the job
                using (var cmd = new SqlCommand("sp_ApplyForJob", con, tran))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@JobId", jobId);
                    cmd.Parameters.AddWithValue("@JobSeekerUserId", jobSeekerUserId);
                    cmd.ExecuteNonQuery();
                }

                // Log the activity
                using (var logCmd = new SqlCommand("sp_LogJobActivity", con, tran))
                {
                    logCmd.CommandType = CommandType.StoredProcedure;
                    logCmd.Parameters.AddWithValue("@JobId", jobId);
                    logCmd.Parameters.AddWithValue("@Action", "Applied");
                    logCmd.Parameters.AddWithValue("@PerformedBy", jobSeekerUserId);
                    logCmd.ExecuteNonQuery();
                }

                tran.Commit();
                Console.WriteLine($"DEBUG: Job application committed successfully for JobId: {jobId}, UserId: {jobSeekerUserId}");

                // Send email notification and create in-app notification (async, after transaction commit)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        Console.WriteLine($"DEBUG: Starting notification process for JobId: {jobId}");

                        var notificationData = await GetJobApplicationNotificationData(jobId, jobSeekerUserId);
                        if (notificationData != null)
                        {
                            Console.WriteLine($"DEBUG: Notification data retrieved for {notificationData.JobTitle}");

                            // Send email notification
                            await _emailService.SendJobApplicationNotificationAsync(notificationData);
                            Console.WriteLine($"DEBUG: Email notification sent to {notificationData.EmployerEmail}");

                            // Create in-app notification
                            var employerUserId = await GetEmployerUserIdByJobId(jobId);
                            if (employerUserId.HasValue)
                            {
                                Console.WriteLine($"DEBUG: Creating in-app notification for employer {employerUserId.Value}");
                                await _notificationService.CreateJobApplicationNotificationAsync(
                                    employerUserId.Value,
                                    jobId,
                                    notificationData.JobTitle,
                                    notificationData.JobSeekerName
                                );
                                Console.WriteLine($"DEBUG: In-app notification created successfully");
                            }
                            else
                            {
                                Console.WriteLine($"DEBUG: Could not find employer user ID for JobId: {jobId}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"DEBUG: Could not retrieve notification data for JobId: {jobId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but don't affect the main flow
                        Console.WriteLine($"DEBUG: Notification failed: {ex.Message}");
                        Console.WriteLine($"DEBUG: Stack trace: {ex.StackTrace}");
                    }
                });
            }
            catch (SqlException ex) when (ex.Message.Contains("Already applied"))
            {
                tran.Rollback();
                throw new InvalidOperationException("You have already applied for this job");
            }
            catch (Exception ex)
            {
                tran.Rollback();
                Console.WriteLine($"DEBUG: Job application failed: {ex.Message}");
                throw;
            }
        }

        // Helper method to get notification data
        private async Task<JobApplicationNotificationDto?> GetJobApplicationNotificationData(int jobId, int jobSeekerUserId)
        {
            using var con = _db.GetConnection();
            using var cmd = new SqlCommand(@"
                SELECT 
                    j.Title as JobTitle,
                    c.CompanyName,
                    emp.Email as EmployerEmail,
                    emp.FirstName + ' ' + emp.LastName as EmployerName,
                    js.Email as JobSeekerEmail,
                    js.FirstName + ' ' + js.LastName as JobSeekerName,
                    GETDATE() as AppliedAt
                FROM Jobs j
                INNER JOIN Users emp ON j.PostedByUserId = emp.UserId
                INNER JOIN CompanyProfiles c ON emp.UserId = c.UserId
                INNER JOIN Users js ON js.UserId = @JobSeekerUserId
                WHERE j.JobId = @JobId", con);

            cmd.Parameters.AddWithValue("@JobId", jobId);
            cmd.Parameters.AddWithValue("@JobSeekerUserId", jobSeekerUserId);

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new JobApplicationNotificationDto
                {
                    JobTitle = reader["JobTitle"].ToString() ?? "",
                    CompanyName = reader["CompanyName"].ToString() ?? "",
                    EmployerEmail = reader["EmployerEmail"].ToString() ?? "",
                    EmployerName = reader["EmployerName"].ToString() ?? "",
                    JobSeekerEmail = reader["JobSeekerEmail"].ToString() ?? "",
                    JobSeekerName = reader["JobSeekerName"].ToString() ?? "",
                    AppliedAt = (DateTime)reader["AppliedAt"]
                };
            }

            return null;
        }

        // JOBSEEKER → VIEW MY APPLICATIONS

        public async Task<List<JobListDto>> GetMyApplications(int jobSeekerUserId)
        {
            var list = new List<JobListDto>();

            using var con = _db.GetConnection();
            using var cmd = new SqlCommand("sp_GetMyApplications", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@JobSeekerUserId", jobSeekerUserId);

            await con.OpenAsync();
            using var rd = await cmd.ExecuteReaderAsync();

            while (await rd.ReadAsync())
            {
                list.Add(new JobListDto
                {
                    JobId = (int)rd["JobId"],
                    Title = rd["Title"].ToString(),
                    Description = rd["Description"].ToString(),
                    RequiredSkills = rd["RequiredSkills"]?.ToString(),
                    Location = rd["Location"]?.ToString(),
                    EmploymentType = rd["EmploymentType"]?.ToString(),
                    PostedBy = rd["PostedBy"].ToString(),
                    CreatedAt = (DateTime)rd["CreatedAt"],
                    ApplicantCount = 0, // Not needed for job seeker view
                    AppliedAt = rd["AppliedAt"] != DBNull.Value ? (DateTime)rd["AppliedAt"] : null,
                    Status = rd["Status"]?.ToString() ?? "Pending"
                });
            }

            return list;
        }

        // EMPLOYER → VIEW APPLICANTS

        public async Task<List<JobApplicantDto>> GetApplicants(int jobId)
        {
            var list = new List<JobApplicantDto>();

            using var con = _db.GetConnection();
            using var cmd = new SqlCommand("sp_GetApplicantsForJob", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@JobId", jobId);

            await con.OpenAsync();
            using var rd = await cmd.ExecuteReaderAsync();

            while (await rd.ReadAsync())
            {
                list.Add(new JobApplicantDto
                {
                    UserId = (int)rd["UserId"],
                    FullName = rd["FirstName"] + " " + rd["LastName"],
                    Email = rd["Email"].ToString(),
                    Skills = rd["Skills"]?.ToString(),
                    Status = rd["Status"].ToString(),
                    AppliedAt = (DateTime)rd["AppliedAt"]
                });
            }

            return list;
        }

        // Helper method to get employer user ID by job ID
        private async Task<int?> GetEmployerUserIdByJobId(int jobId)
        {
            using var con = _db.GetConnection();
            using var cmd = new SqlCommand("SELECT PostedByUserId FROM Jobs WHERE JobId = @JobId", con);
            cmd.Parameters.AddWithValue("@JobId", jobId);

            await con.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();
            return result as int?;
        }

        // EMPLOYER → UPDATE APPLICATION STATUS
        public async Task UpdateApplicationStatus(int jobId, int jobSeekerUserId, UpdateApplicationStatusDto dto, int updatedBy)
        {
            using var con = _db.GetConnection();
            con.Open();

            using var tran = con.BeginTransaction();

            try
            {
                using (var cmd = new SqlCommand("sp_UpdateApplicationStatus", con, tran))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@JobId", jobId);
                    cmd.Parameters.AddWithValue("@JobSeekerUserId", jobSeekerUserId);
                    cmd.Parameters.AddWithValue("@Status", dto.Status);
                    cmd.Parameters.AddWithValue("@Notes", dto.Notes ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@UpdatedBy", updatedBy);

                    await cmd.ExecuteNonQueryAsync();
                }

                tran.Commit();

                // Send notification to job seeker (async, after transaction commit)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificationService.CreateApplicationStatusNotificationAsync(
                            jobSeekerUserId,
                            jobId,
                            dto.Status
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to send status notification: {ex.Message}");
                    }
                });
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }
    }
}