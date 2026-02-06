using AuthSystemApi.DTOs;
using AuthSystemApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthSystemApi.Controllers
{
    [ApiController]
    [Route("api/jobs")]
    public class JobsController : ControllerBase
    {
        private readonly IJobService _service;

        public JobsController(IJobService service)
        {
            _service = service;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        // EMPLOYER → POST JOB
        [HttpPost]
        [Authorize(Roles = "Employer")]
        public IActionResult CreateJob(CreateJobDto dto)
        {
            _service.CreateJob(GetUserId(), dto);
            return Ok("Job posted successfully");
        }

        // JOBSEEKER → VIEW JOBS
        [HttpGet]
        [Authorize(Roles = "JobSeeker")]
        public async Task<IActionResult> GetOpenJobs()
        {
            var data = await _service.GetOpenJobs();
            return Ok(data);
        }

        // EMPLOYER → VIEW MY JOBS
        [HttpGet("my-jobs")]
        [Authorize(Roles = "Employer")]
        public async Task<IActionResult> GetMyJobs()
        {
            var data = await _service.GetMyJobs(GetUserId());
            return Ok(data);
        }

        // JOBSEEKER → VIEW MY APPLICATIONS
        [HttpGet("my-applications")]
        [Authorize(Roles = "JobSeeker")]
        public async Task<IActionResult> GetMyApplications()
        {
            var data = await _service.GetMyApplications(GetUserId());
            return Ok(data);
        }

        // JOBSEEKER → APPLY JOB
        [HttpPost("{jobId}/apply")]
        [Authorize(Roles = "JobSeeker")]
        public IActionResult ApplyForJob(int jobId)
        {
            try
            {
                _service.ApplyForJob(jobId, GetUserId());
                return Ok("Applied successfully");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already applied"))
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while processing your application");
            }
        }

        // EMPLOYER → VIEW APPLICANTS
        [HttpGet("{jobId}/applicants")]
        [Authorize(Roles = "Employer")]
        public async Task<IActionResult> GetApplicants(int jobId)
        {
            var data = await _service.GetApplicants(jobId);
            return Ok(data);
        }

        // EMPLOYER → UPDATE APPLICATION STATUS
        [HttpPut("{jobId}/applicants/{jobSeekerUserId}/status")]
        [Authorize(Roles = "Employer")]
        public async Task<IActionResult> UpdateApplicationStatus(int jobId, int jobSeekerUserId, UpdateApplicationStatusDto dto)
        {
            try
            {
                await _service.UpdateApplicationStatus(jobId, jobSeekerUserId, dto, GetUserId());
                return Ok("Application status updated successfully");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
