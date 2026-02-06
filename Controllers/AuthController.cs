using AuthSystemApi.DTOs;
using AuthSystemApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AuthSystemApi.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;

        public AuthController(IAuthService auth)
        {
            _auth = auth;
        }

        [HttpPost("register")]
        public IActionResult Register(RegisterRequest req)
        {
            _auth.Register(req);
            return Ok("Registered Successfully");
        }

        [HttpPost("login")]
        public IActionResult Login(LoginRequest req)
        {
            var token = _auth.Login(req);
            if (token == null)
                return Unauthorized("Invalid email or password");

            return Ok(new { token });
        }
        [HttpPost("reset-password")]
        public IActionResult ResetPassword(ResetPasswordDto dto)
        {
            var result = _auth.ResetPassword(dto.Email, dto.NewPassword);

            if (!result)
                return BadRequest("User not found");

            return Ok("Password updated successfully");
        }


    }
}
