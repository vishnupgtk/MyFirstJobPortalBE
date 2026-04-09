using AuthSystemApi.DTOs;

namespace AuthSystemApi.Services.Interfaces
{
    public interface IJobSeekerService
    {
        JobSeekerProfileViewDto GetProfile(int userId);
        void UpdateProfile(JobSeekerProfileUpdateDto dto);

        Task<List<JobSeekerChangeHistoryDto>> GetHistory(int userId);

        Task<List<JobSeekerChangeHistoryDto>> GetAllHistory();

        Task<string> UploadResume(int userId, IFormFile file);
        Task DeleteResume(int userId);
        Task UpdateProfileFromParsedResume(int userId, ResumeParseResponseDto parsedResume, string fileName, string filePath);
    }
}

