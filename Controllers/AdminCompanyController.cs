using AuthSystemApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthSystemApi.Controllers
{
    [ApiController]
    [Route("api/admin/company")]
    [Authorize(Roles = "Admin")]
    public class AdminCompanyController : ControllerBase
    {
        private readonly ICompanyService _companyService;

        public AdminCompanyController(ICompanyService companyService)
        {
            _companyService = companyService;
        }

        //  Admin can view employer company profile 
        [HttpGet("{userId}")]
        public IActionResult GetCompanyProfileByUserId(int userId)
        {
            var profile = _companyService.GetProfile(userId);

            if (profile == null)
                return NotFound("Company profile not found");

            return Ok(profile);
        }
    }
}
