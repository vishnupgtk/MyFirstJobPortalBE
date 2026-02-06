using AuthSystemApi.DTOs;

namespace AuthSystemApi.Services.Interfaces
{
    public interface IJobSeekerService
    {
        JobSeekerProfileViewDto GetProfile(int userId);
        void UpdateProfile(JobSeekerProfileUpdateDto dto);

        Task<List<JobSeekerChangeHistoryDto>> GetHistory(int userId);

        Task<List<JobSeekerChangeHistoryDto>> GetAllHistory();

    }
}
