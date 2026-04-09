using AuthSystemApi.DTOs;
using AuthSystemApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthSystemApi.Controllers
{
    [ApiController]
    [Route("api/resume-parsing")]
    public class ResumeParsingController : ControllerBase
    {
        private readonly ResumeParsingService _resumeParsingService;
        private readonly JobDescriptionParsingService _jobDescriptionParsingService;
        private readonly MatchingService _matchingService;

        public ResumeParsingController(
            ResumeParsingService resumeParsingService,
            JobDescriptionParsingService jobDescriptionParsingService,
            MatchingService matchingService)
        {
            _resumeParsingService = resumeParsingService;
            _jobDescriptionParsingService = jobDescriptionParsingService;
            _matchingService = matchingService;
        }

        [HttpPost("parse-resume")]
        [Authorize]
        public IActionResult ParseResume([FromBody] ResumeParseRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.ResumeText))
                return BadRequest("Resume text is required");

            var result = _resumeParsingService.ParseResume(request.ResumeText);
            return Ok(result);
        }

        [HttpPost("parse-job-description")]
        [Authorize(Roles = "Employer")]
        public IActionResult ParseJobDescription([FromBody] JobDescriptionParseRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.JobDescriptionText))
                return BadRequest("Job description text is required");

            var result = _jobDescriptionParsingService.ParseJobDescription(request.JobDescriptionText);
            return Ok(result);
        }

        [HttpPost("calculate-match")]
        [Authorize(Roles = "Employer")]
        public IActionResult CalculateMatch([FromBody] MatchingScoreRequestDto request)
        {
            if (request.Resume == null || request.JobDescription == null)
                return BadRequest("Both resume and job description are required");

            var result = _matchingService.CalculateMatchScore(request.Resume, request.JobDescription);
            return Ok(result);
        }

        [HttpPost("upload-and-parse-resume")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAndParseResume([FromForm] FileUploadRequestDto request)
        {
            try
            {
                var fileProcessingService = HttpContext.RequestServices.GetRequiredService<ResumeFileProcessingService>();
                var file = request.File;

                if (!fileProcessingService.IsValidResumeFile(file))
                    return BadRequest("Invalid file. Please upload a PDF, DOCX, DOC, or TXT file (max 10MB)");

                var parsedResume = await fileProcessingService.ProcessResumeFileAsync(file);
                return Ok(parsedResume);
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
    }
}
