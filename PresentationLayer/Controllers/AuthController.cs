using BusinessLayer.DTOs;
using BusinessLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register-citizen")]
        public async Task<IActionResult> RegisterCitizen([FromBody] RegisterCitizenDto registerDto)
        {

            if (!ModelState.IsValid)
            {

                return BadRequest(ModelState);
            }
            var result = await _authService.RegisterCitizenAsync(registerDto);
            if (!result.Success)
                return BadRequest(result.Errors);

            return Ok(result);
        }




        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var result = await _authService.LoginAsync(loginDto);
            if (!result.Success)
                return Unauthorized(result);
            return Ok(result);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenRequestDto tokenRequestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResult
                {
                    Success = false,
                    Errors = new List<string> { "Invalid token request" }
                });
            }
            var result = await _authService.RefreshTokenAsync(tokenRequestDto);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
