using CourseProject.Application.Interfaces;
using CourseProject.Application.Models;
using CourseProject.Application.Services;
using CourseProject.Presentation.Interfaces;
using CourseProject.Presentation.Models;
using CourseProject.Presentation.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseProject.Presentation.Controllers
{
    [Route("auth")]
    [ApiController]
    public class AuthController : Controller
    {
        private readonly IUserService _userService;
        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Register new user
        /// </summary>
        /// <returns></returns>
        /// <response code="201">User created successfully</response>
        /// <response code="400">Invalid user's data</response>

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel registerModel)
        {
            var userId = await _userService.RegisterUserAsync(registerModel.Login, registerModel.Password, registerModel.Role);

            return NoContent();
        }

        /// <summary>
        /// Login user
        /// </summary>
        /// <returns></returns>
        /// <response code="200">OK</response>
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel loginModel)
        {
            var token = await _userService.LoginUserAsync(loginModel.Login, loginModel.Password);

            return Ok(new { token = token });
        }

    }
}
