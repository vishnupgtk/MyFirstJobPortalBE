using AuthSystemApi.DTOs;

namespace AuthSystemApi.Services.Interfaces
{
    public interface IJobService
    {
        // EMPLOYEE
        void CreateJob(int userId, CreateJobDto dto);
        Task<List<JobListDto>> GetMyJobs(int userId);
        Task CloseJob(int userId, int jobId);

        // JOBSEEKER
        Task<List<JobListDto>> GetOpenJobs();
        void ApplyForJob(int jobId, int jobSeekerUserId);
        Task<List<JobListDto>> GetMyApplications(int jobSeekerUserId);

        // EMPLOYEE
        Task<List<JobApplicantDto>> GetApplicants(int jobId);
        Task<List<JobApplicantWithScoreDto>> GetApplicantsWithMatchingScore(int jobId);
        Task UpdateApplicationStatus(int jobId, int jobSeekerUserId, UpdateApplicationStatusDto dto, int updatedBy);

        // EMPLOYER DASHBOARD
        Task<EmployerDashboardMetricsDto> GetDashboardMetrics(int userId);
        Task<List<DashboardJobDto>> GetDashboardJobsOverview(int userId);
        Task<List<RecentApplicantDto>> GetRecentApplicants(int userId);
    }
}
