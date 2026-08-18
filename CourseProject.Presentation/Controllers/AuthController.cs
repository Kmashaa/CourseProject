using CourseProject.Application.Interfaces;
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
        /// <response code=""></response>
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel registerModel)
        {
            var token = await _userService.RegisterUserAsync(registerModel.Login, registerModel.Password, registerModel.Role);

            return Ok(token); // 201 Created //TODO вид ответа
        }

        /// <summary>
        /// Login user
        /// </summary>
        /// <returns></returns>
        /// <response code=""></response>
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel loginModel)
        {
            var token = await _userService.LoginUserAsync(loginModel.Login, loginModel.Password);

            return Ok(token); // 201 Created //TODO вид ответа
        }

    }
}
