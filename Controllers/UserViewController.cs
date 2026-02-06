using AuthSystemApi.Services.Interfaces;
using AuthSystemApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace AuthSystemApi.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize(Roles = "Admin,Employer,JobSeeker")]
    public class UserViewController : ControllerBase
    {
        private readonly IUserService _service;

        public UserViewController(IUserService service)
        {
            _service = service;
        }

        //GET ALL JOB SEEKERS (Admin / Employer)
        [HttpGet("jobseekers")]
        [Authorize(Roles = "Admin,Employer")]
        public IActionResult GetJobSeekers()
        {
            var users = _service.GetAllUsers()
                .Where(u => u.RoleName == "JobSeeker");

            return Ok(users);
        }

        // GET ALL EMPLOYERS (JobSeeker / Admin)
        [HttpGet("employers")]
        [Authorize(Roles = "Admin,JobSeeker")]
        public IActionResult GetEmployers()
        {
            var users = _service.GetAllUsers()
                .Where(u => u.RoleName == "Employer");

            return Ok(users);
        }
    }
}
