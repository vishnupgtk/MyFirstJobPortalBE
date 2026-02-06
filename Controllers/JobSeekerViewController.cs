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

        public JobSeekerViewController(IJobSeekerService service)
        {
            _service = service;
        }

        [HttpGet("{userId}")]
        public IActionResult ViewProfile(int userId)
        {
            return Ok(_service.GetProfile(userId));
        }
    }
}
