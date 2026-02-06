using AuthSystemApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthSystemApi.Controllers
{
    [ApiController]
    [Route("api/admin/jobseekers")]
    [Authorize(Roles = "Admin")]
    public class AdminJobSeekerController : ControllerBase
    {
        private readonly IJobSeekerService _service;

        public AdminJobSeekerController(IJobSeekerService service)
        {
            _service = service;
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetAllHistory()
        {
            var data = await _service.GetAllHistory();
            return Ok(data);
        }
    }

}
