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
        private readonly ResumeParsingService _resumeParsingService;
        private readonly JobDescriptionParsingService _jobDescriptionParsingService;
        private readonly MatchingService _matchingService;

        public JobService(DbHelper db, IEmailService emailService, INotificationService notificationService,
            ResumeParsingService resumeParsingService, JobDescriptionParsingService jobDescriptionParsingService,
            MatchingService matchingService)
        {
            _db = db;
            _emailService = emailService;
            _notificationService = notificationService;
            _resumeParsingService = resumeParsingService;
            _jobDescriptionParsingService = jobDescriptionParsingService;
            _matchingService = matchingService;
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
            await con.OpenAsync();
            var hasStatus = await HasJobsColumn(con, "Status");
            var hasIsActive = await HasJobsColumn(con, "IsActive");
            var hasIsDeleted = await HasJobsColumn(con, "IsDeleted");

            var statusExpression = hasStatus
                ? "CAST(j.Status AS NVARCHAR(50))"
                : hasIsActive
                    ? "CASE WHEN j.IsActive = 1 THEN 'Active' ELSE 'Closed' END"
                    : "'Active'";
            var deletedFilter = hasIsDeleted ? "AND j.IsDeleted = 0" : "";

            var sql = $@"
                SELECT
                    j.JobId,
                    j.Title,
                    j.Description,
                    j.RequiredSkills,
                    j.Location,
                    j.EmploymentType,
                    CONCAT(u.FirstName, ' ', u.LastName) AS PostedBy,
                    j.CreatedAt,
                    (SELECT COUNT(*) FROM JobApplications ja WHERE ja.JobId = j.JobId) AS ApplicantCount,
                    {statusExpression} AS JobStatus
                FROM Jobs j
                INNER JOIN Users u ON j.PostedByUserId = u.UserId
                WHERE j.PostedByUserId = @UserId
                {deletedFilter}
                ORDER BY j.CreatedAt DESC";

            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@UserId", userId);

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
                    ApplicantCount = rd["ApplicantCount"] != DBNull.Value ? (int)rd["ApplicantCount"] : 0,
                    Status = rd["JobStatus"]?.ToString() ?? "Active"
                });
            }

            return list;
        }

        public async Task CloseJob(int userId, int jobId)
        {
            using var con = _db.GetConnection();
            await con.OpenAsync();

            using var tran = con.BeginTransaction();
            try
            {
                using var verifyCmd = new SqlCommand(@"
                    DECLARE @canClose BIT = 0;

                    IF COL_LENGTH('Jobs', 'IsDeleted') IS NOT NULL
                    BEGIN
                        IF EXISTS (
                            SELECT 1
                            FROM Jobs
                            WHERE JobId = @JobId
                              AND PostedByUserId = @UserId
                              AND IsDeleted = 0
                        )
                        BEGIN
                            SET @canClose = 1;
                        END
                    END
                    ELSE
                    BEGIN
                        IF EXISTS (
                            SELECT 1
                            FROM Jobs
                            WHERE JobId = @JobId
                              AND PostedByUserId = @UserId
                        )
                        BEGIN
                            SET @canClose = 1;
                        END
                    END

                    SELECT @canClose;", con, tran);
                verifyCmd.Parameters.AddWithValue("@JobId", jobId);
                verifyCmd.Parameters.AddWithValue("@UserId", userId);

                var canClose = (bool)(await verifyCmd.ExecuteScalarAsync() ?? false);
                if (!canClose)
                {
                    throw new InvalidOperationException("Job not found");
                }

                using var closeCmd = new SqlCommand(@"
                    IF COL_LENGTH('Jobs', 'Status') IS NOT NULL
                    BEGIN
                        UPDATE Jobs
                        SET Status = 'Closed',
                            UpdatedAt = GETDATE()
                        WHERE JobId = @JobId;
                    END
                    ELSE IF COL_LENGTH('Jobs', 'IsActive') IS NOT NULL
                    BEGIN
                        UPDATE Jobs
                        SET IsActive = 0,
                            UpdatedAt = GETDATE()
                        WHERE JobId = @JobId;
                    END
                    ELSE
                    BEGIN
                        THROW 50001, 'No closable job state column found.', 1;
                    END", con, tran);
                closeCmd.Parameters.AddWithValue("@JobId", jobId);
                await closeCmd.ExecuteNonQueryAsync();

                using var logCmd = new SqlCommand("sp_LogJobActivity", con, tran);
                logCmd.CommandType = CommandType.StoredProcedure;
                logCmd.Parameters.AddWithValue("@JobId", jobId);
                logCmd.Parameters.AddWithValue("@Action", "Closed");
                logCmd.Parameters.AddWithValue("@PerformedBy", userId);
                await logCmd.ExecuteNonQueryAsync();

                await tran.CommitAsync();
            }
            catch
            {
                await tran.RollbackAsync();
                throw;
            }
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

        // EMPLOYER → VIEW APPLICANTS WITH MATCHING SCORES

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

        // EMPLOYER → VIEW APPLICANTS WITH MATCHING SCORES (SORTED BY MATCH)
        public async Task<List<JobApplicantWithScoreDto>> GetApplicantsWithMatchingScore(int jobId)
        {
            var applicants = new List<JobApplicantWithScoreDto>();

            // Get job description
            var jobDescription = await GetJobDescription(jobId);
            if (jobDescription == null)
                return applicants;

            // Parse job description
            var parsedJobDescription = _jobDescriptionParsingService.ParseJobDescription(jobDescription);

            using var con = _db.GetConnection();
            using var cmd = new SqlCommand(@"
                SELECT 
                    u.UserId,
                    u.FirstName + ' ' + u.LastName as FullName,
                    u.Email,
                    jsp.Skills,
                    ja.Status,
                    ja.AppliedAt,
                    jsp.Summary,
                    jsp.Education,
                    jsp.College,
                    jsp.ResumeFilePath
                FROM JobApplications ja
                INNER JOIN Users u ON ja.JobSeekerUserId = u.UserId
                LEFT JOIN JobSeekerProfiles jsp ON u.UserId = jsp.UserId
                WHERE ja.JobId = @JobId
                ORDER BY ja.AppliedAt DESC", con);

            cmd.Parameters.AddWithValue("@JobId", jobId);

            await con.OpenAsync();
            using var rd = await cmd.ExecuteReaderAsync();

            while (await rd.ReadAsync())
            {
                var applicant = new JobApplicantWithScoreDto
                {
                    UserId = (int)rd["UserId"],
                    FullName = rd["FullName"].ToString() ?? "",
                    Email = rd["Email"].ToString() ?? "",
                    Skills = rd["Skills"]?.ToString() ?? "",
                    Status = rd["Status"].ToString() ?? "Pending",
                    AppliedAt = (DateTime)rd["AppliedAt"]
                };

                // Build resume text from profile data
                var resumeText = BuildResumeText(
                    applicant.FullName,
                    applicant.Email,
                    rd["Summary"]?.ToString(),
                    rd["Education"]?.ToString(),
                    rd["College"]?.ToString(),
                    applicant.Skills
                );

                // Parse resume and calculate match
                var parsedResume = _resumeParsingService.ParseResume(resumeText);
                var matchScore = _matchingService.CalculateMatchScore(parsedResume, parsedJobDescription);

                applicant.MatchPercentage = matchScore.MatchPercentage;
                applicant.MatchDetails = matchScore;

                applicants.Add(applicant);
            }

            // Sort by match percentage (highest first)
            return applicants.OrderByDescending(a => a.MatchPercentage ?? 0).ToList();
        }

        private async Task<string?> GetJobDescription(int jobId)
        {
            using var con = _db.GetConnection();
            using var cmd = new SqlCommand(@"
                SELECT Title, Description, RequiredSkills, ExperienceLevel, Location, EmploymentType
                FROM Jobs WHERE JobId = @JobId", con);
            cmd.Parameters.AddWithValue("@JobId", jobId);

            await con.OpenAsync();
            using var rd = await cmd.ExecuteReaderAsync();

            if (await rd.ReadAsync())
            {
                return $@"
Job Title: {rd["Title"]}
Description: {rd["Description"]}
Required Skills: {rd["RequiredSkills"]}
Experience Level: {rd["ExperienceLevel"]}
Location: {rd["Location"]}
Employment Type: {rd["EmploymentType"]}";
            }

            return null;
        }

        private string BuildResumeText(string fullName, string email, string? summary, string? education, string? college, string? skills)
        {
            return $@"
{fullName}
{email}

Summary: {summary ?? "N/A"}
Education: {education ?? "N/A"}
College: {college ?? "N/A"}
Skills: {skills ?? "N/A"}";
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

        // EMPLOYER DASHBOARD METRICS
        public async Task<EmployerDashboardMetricsDto> GetDashboardMetrics(int userId)
        {
            using var con = _db.GetConnection();
            using var cmd = new SqlCommand("sp_GetEmployerDashboardMetrics", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserId", userId);

            await con.OpenAsync();
            using var rd = await cmd.ExecuteReaderAsync();

            if (await rd.ReadAsync())
            {
                return new EmployerDashboardMetricsDto
                {
                    ActiveJobs = (int)rd["ActiveJobs"],
                    TotalApplicants = (int)rd["TotalApplicants"],
                    NewApplicants = (int)rd["NewApplicants"],
                    ShortlistedCandidates = (int)rd["ShortlistedCandidates"],
                    InterviewsScheduled = (int)rd["InterviewsScheduled"]
                };
            }

            return new EmployerDashboardMetricsDto();
        }

        public async Task<List<DashboardJobDto>> GetDashboardJobsOverview(int userId)
        {
            var list = new List<DashboardJobDto>();

            using var con = _db.GetConnection();
            using var cmd = new SqlCommand("sp_GetDashboardJobsOverview", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserId", userId);

            await con.OpenAsync();
            using var rd = await cmd.ExecuteReaderAsync();

            while (await rd.ReadAsync())
            {
                list.Add(new DashboardJobDto
                {
                    JobId = (int)rd["JobId"],
                    Title = rd["Title"].ToString() ?? "",
                    ApplicantCount = (int)rd["ApplicantCount"],
                    NewApplicantCount = (int)rd["NewApplicantCount"],
                    Status = rd["Status"].ToString() ?? "Active",
                    CreatedAt = (DateTime)rd["CreatedAt"]
                });
            }

            return list;
        }

        public async Task<List<RecentApplicantDto>> GetRecentApplicants(int userId)
        {
            var list = new List<RecentApplicantDto>();

            using var con = _db.GetConnection();
            using var cmd = new SqlCommand("sp_GetRecentApplicants", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserId", userId);

            await con.OpenAsync();
            using var rd = await cmd.ExecuteReaderAsync();

            while (await rd.ReadAsync())
            {
                list.Add(new RecentApplicantDto
                {
                    UserId = (int)rd["UserId"],
                    FullName = rd["FullName"].ToString() ?? "",
                    JobTitle = rd["JobTitle"].ToString() ?? "",
                    JobId = (int)rd["JobId"],
                    AppliedAt = (DateTime)rd["AppliedAt"],
                    Status = rd["Status"].ToString() ?? "Pending"
                });
            }

            return list;
        }

        private static async Task<bool> HasJobsColumn(SqlConnection connection, string columnName)
        {
            using var cmd = new SqlCommand("SELECT CASE WHEN COL_LENGTH('Jobs', @ColumnName) IS NULL THEN 0 ELSE 1 END", connection);
            cmd.Parameters.AddWithValue("@ColumnName", columnName);
            var result = await cmd.ExecuteScalarAsync();
            return result != null && Convert.ToInt32(result) == 1;
        }
    }
}
