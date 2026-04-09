using System.Security.Claims;
using AuthSystemApi.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthSystemApi.Controllers
{
    [ApiController]
    [Route("api/chatbot")]
    [Authorize(Roles = "Employer,JobSeeker")]
    public class ChatbotController : ControllerBase
    {
        private readonly AuthSystemApi.Services.Interfaces.IChatbotService _chatbotService;

        public ChatbotController(AuthSystemApi.Services.Interfaces.IChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        [HttpPost("message")]
        public async Task<IActionResult> SendMessage([FromBody] SendChatMessageDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userType = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";

            var response = await _chatbotService.ProcessMessageAsync(userId, userType, dto);
            return Ok(response);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] string? sessionId = null)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var history = await _chatbotService.GetChatHistoryAsync(userId, sessionId);
            return Ok(history);
        }
    }
}
