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

        public JobSeekerController(IJobSeekerService service)
        {
            _service = service;
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
    }
}
