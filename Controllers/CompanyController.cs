using AuthSystemApi.DTOs;
using AuthSystemApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthSystemApi.Controllers
{
    [ApiController]
    [Route("api/company")]
    public class CompanyController : ControllerBase
    {
        private readonly ICompanyService _service;

        public CompanyController(ICompanyService service)
        {
            _service = service;
        }

        // Employer & Admin → get profile
        [Authorize]
        [HttpGet("me")]
        public IActionResult MyProfile()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return Ok(_service.GetProfile(userId));
        }

        // Employer → request change
        [Authorize(Roles = "Employer")]
        [HttpPost("request-change")]
        public async Task<IActionResult> RequestChange(CompanyChangeRequestDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _service.RequestProfileChange(dto.CompanyId, dto.FieldName, dto.NewValue, userId);
            return Ok("Change request submitted");
        }

        // Admin → view pending
        [Authorize(Roles = "Admin")]
        [HttpGet("pending")]
        public async Task<IActionResult> Pending()
        {
            return Ok(await _service.GetPendingRequests());
        }

        // Admin → approve
        [Authorize(Roles = "Admin")]
        [HttpPost("approve")]
        public async Task<IActionResult> Approve(ApproveChangeDto dto)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _service.ApproveChange(dto.RequestId, adminId);
            return Ok("Approved");
        }

        // Admin → reject
        [Authorize(Roles = "Admin")]
        [HttpPost("reject")]
        public async Task<IActionResult> Reject(ApproveChangeDto dto)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _service.RejectChange(dto.RequestId, adminId);
            return Ok("Rejected");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("history/all")]
        public async Task<IActionResult> AllHistory()
        {
            return Ok(await _service.GetAllHistory());
        }


        // History
        [Authorize]
        [HttpGet("{companyId}/history")]
        public async Task<IActionResult> History(int companyId)
        {
            return Ok(await _service.GetCompanyHistory(companyId));
        }
    }
}
