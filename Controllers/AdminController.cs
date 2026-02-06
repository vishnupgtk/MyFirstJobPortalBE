using AuthSystemApi.DTOs;
using AuthSystemApi.Services;
using AuthSystemApi.Services.Interfaces;
using AuthSystemApi.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthSystemApi.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly IUserService _service;

        public AdminController(IUserService service)
        {
            _service = service;
        }

        [HttpGet("users")]
        public IActionResult GetAll()
        {
            return Ok(_service.GetAllUsers());
        }

        [HttpGet("users/paginated")]
        public IActionResult GetUsersPaginated([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var result = _service.GetUsersPaginated(pageNumber, pageSize);
            return Ok(result);
        }

        // TEST: Custom Auth on one endpoint (safe to test)
        [AdminOnly]
        [HttpGet("users-custom-auth")]
        public IActionResult GetAllWithCustomAuth()
        {
            return Ok(new
            {
                message = "Custom Admin Auth working on real endpoint!",
                users = _service.GetAllUsers(),
                timestamp = DateTime.UtcNow
            });
        }

        [HttpGet("users/{id}")]
        public IActionResult GetById(int id)
        {
            var user = _service.GetUserById(id);
            if (user == null)
                return NotFound("User not found");

            return Ok(user);
        }

        [HttpPost("users")]
        public IActionResult Create([FromBody] RegisterRequest req)
        {
            _service.CreateUser(req);
            return Ok("User created successfully");
        }

        [HttpPut("users/{id}")]
        public IActionResult Update(int id, [FromBody] UpdateUserDto dto)
        {
            if (id != dto.UserId)
                return BadRequest("UserId mismatch");

            _service.UpdateUser(dto);
            return Ok("User updated successfully");
        }

        [HttpDelete("users/{id}")]
        public IActionResult Delete(int id)
        {
            _service.DeleteUser(id);
            return Ok("User deleted successfully");
        }
    }
}
