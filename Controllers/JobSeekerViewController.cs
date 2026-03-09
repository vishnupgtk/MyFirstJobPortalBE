using AuthSystemApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthSystemApi.Controllers
{
    [ApiController]
    [Route("api/view/jobseeker")]
    [Authorize(Roles = "Admin,Employer")]
    public class JobSeekerViewController : ControllerBase
    {
        private readonly IJobSeekerService _service;
        private readonly IWebHostEnvironment _env;

        public JobSeekerViewController(IJobSeekerService service, IWebHostEnvironment env)
        {
            _service = service;
            _env = env;
        }

        [HttpGet("{userId}")]
        public IActionResult ViewProfile(int userId)
        {
            return Ok(_service.GetProfile(userId));
        }

        // DOWNLOAD RESUME (for employers/admin)
        [HttpGet("{userId}/resume")]
        public IActionResult DownloadResume(int userId)
        {
            var profile = _service.GetProfile(userId);

            if (string.IsNullOrEmpty(profile.ResumeFilePath))
                return NotFound(new { message = "No resume found for this job seeker" });

            var filePath = Path.Combine(_env.ContentRootPath, "Uploads", "Resumes", profile.ResumeFilePath);

            if (!System.IO.File.Exists(filePath))
                return NotFound(new { message = "Resume file not found" });

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            var contentType = "application/octet-stream";

            return File(fileBytes, contentType, profile.ResumeFileName);
        }
    }
}
