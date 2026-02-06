using AuthSystemApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthSystemApi.Controllers
{
    [ApiController]
    [Route("api/view/company")]
    [Authorize(Roles = "Admin,JobSeeker")]
    public class CompanyViewController : ControllerBase
    {
        private readonly ICompanyService _service;

        public CompanyViewController(ICompanyService service)
        {
            _service = service;
        }

        //READ-ONLY company profile
        [HttpGet("{userId}")]
        public IActionResult ViewCompanyProfile(int userId)
        {
            var profile = _service.GetProfile(userId);

            if (profile == null)
                return NotFound("Company profile not found");

            return Ok(profile);
        }
    }
}
