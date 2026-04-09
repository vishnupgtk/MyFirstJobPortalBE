using AuthSystemApi.DTOs;
using AuthSystemApi.Services.Interfaces;
using AuthSystemApi.Services;
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
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadResume([FromForm] FileUploadRequestDto request)
        {
            try
            {
                var fileName = await _service.UploadResume(GetUserId(), request.File);
                return Ok(new { message = "Resume uploaded successfully", fileName });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // UPLOAD AND PARSE RESUME
        [HttpPost("resume/upload-and-parse")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAndParseResume([FromForm] FileUploadRequestDto request)
        {
            try
            {
                var fileProcessingService = HttpContext.RequestServices.GetRequiredService<ResumeFileProcessingService>();
                var file = request.File;

                if (!fileProcessingService.IsValidResumeFile(file))
                    return BadRequest("Invalid file. Please upload a PDF, DOCX, DOC, or TXT file (max 10MB)");

                // Save the file
                var filePath = await fileProcessingService.SaveResumeFileAsync(file, GetUserId());

                // Parse the resume
                var parsedResume = await fileProcessingService.ProcessResumeFileAsync(file);

                // Update the job seeker profile with parsed data
                await _service.UpdateProfileFromParsedResume(GetUserId(), parsedResume, file.FileName, filePath);

                return Ok(new
                {
                    message = "Resume uploaded and parsed successfully",
                    fileName = file.FileName,
                    parsedData = parsedResume
                });
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error processing resume: {ex.Message}");
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
