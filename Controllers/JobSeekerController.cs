using AuthSystemApi.DTOs;
using AuthSystemApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthSystemApi.Controllers
{
    [ApiController]
    [Route("api/jobseeker")]
    [Authorize(Roles = "JobSeeker")]
    public class JobSeekerController : ControllerBase
    {
        private readonly IJobSeekerService _service;
        private readonly IWebHostEnvironment _env;

        public JobSeekerController(IJobSeekerService service, IWebHostEnvironment env)
        {
            _service = service;
            _env = env;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        // GET profile
        [HttpGet("profile")]
        public IActionResult GetProfile()
        {
            var data = _service.GetProfile(GetUserId());
            return Ok(data);
        }

        // UPDATE profile (this triggers audit logging in SP)
        [HttpPut("profile")]
        public IActionResult UpdateProfile(JobSeekerProfileUpdateDto dto)
        {
            dto.UserId = GetUserId();   // secure
            _service.UpdateProfile(dto);
            return Ok("Profile updated");
        }


        //  HISTORY (audit log)
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var data = await _service.GetHistory(GetUserId());
            return Ok(data);
        }

        // UPLOAD RESUME
        [HttpPost("resume")]
        public async Task<IActionResult> UploadResume(IFormFile file)
        {
            try
            {
                var fileName = await _service.UploadResume(GetUserId(), file);
                return Ok(new { message = "Resume uploaded successfully", fileName });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE RESUME
        [HttpDelete("resume")]
        public async Task<IActionResult> DeleteResume()
        {
            await _service.DeleteResume(GetUserId());
            return Ok(new { message = "Resume deleted successfully" });
        }

        // DOWNLOAD RESUME
        [HttpGet("resume/download")]
        public IActionResult DownloadResume()
        {
            var profile = _service.GetProfile(GetUserId());

            if (string.IsNullOrEmpty(profile.ResumeFilePath))
                return NotFound(new { message = "No resume found" });

            var filePath = Path.Combine(_env.ContentRootPath, "Uploads", "Resumes", profile.ResumeFilePath);

            if (!System.IO.File.Exists(filePath))
                return NotFound(new { message = "Resume file not found" });

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            var contentType = "application/octet-stream";

            return File(fileBytes, contentType, profile.ResumeFileName);
        }
    }
}
