using BusinessLayer.DTOs;
using BusinessLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
        [EnableRateLimiting("LoginLimit")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 1. Apelăm serviciul exact cum o făceai și înainte
            var result = await _authService.LoginAsync(loginDto);

            if (!result.Success)
            {
                return Unauthorized(result);
            }

            // 2. Creăm setările pentru Cookie-ul securizat
            var jwtcookieOptions = new CookieOptions
            {
                HttpOnly = true, // Securitate maximă: JavaScript-ul (React) NU poate citi acest cookie (previne atacurile XSS)
                Secure = true,   // Cookie-ul este trimis doar prin HTTPS (dacă rulezi local, ASP.NET Core știe să gestioneze asta pe https://localhost)
                SameSite = SameSiteMode.None, // Permite trimiterea cookie-ului între porturi diferite (ex: de la portul 5173 de React la portul de .NET)
                Expires = DateTime.UtcNow.AddMinutes(5) // Ajustează aici ca să coincidă cu durata de viață a token-ului tău
            };

            // 3. Atașăm token-ul în cookie sub numele "jwt"
            // ATENȚIE: Asigură-te că "result.Token" este numele corect al proprietății din clasa ta care conține string-ul JWT.
            Response.Cookies.Append("jwt", result.Token, jwtcookieOptions);

            var refreshCookieOptions = new CookieOptions
            {
                HttpOnly = true, // BLINDAT: JS nu poate citi nici acest token!
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(7)
            };
            Response.Cookies.Append("refreshToken", result.RefreshToken, refreshCookieOptions);
            // 4. Returnăm doar informațiile utile, FĂRĂ să mai expunem token-ul în JSON-ul din body!
            return Ok(new { Success = true, Message = "Autentificare cu succes!", Role=result.Role, FullName=result.FullName });
        }



        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {

            if (Request.Cookies.TryGetValue("refreshToken", out var incomingRefreshToken))
            {
                await _authService.RevokeTokenAsync(incomingRefreshToken);
            }
            Response.Cookies.Delete("jwt");
            Response.Cookies.Delete("refreshToken");
            return Ok(new { Success = true, Message = "Deconectare cu succes!" });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken() // Nu mai avem [FromBody] aici
        {
            // 1. Extragem Refresh Token-ul din Cookie-ul ascuns trimis de browser
            if (!Request.Cookies.TryGetValue("refreshToken", out var incomingRefreshToken))
            {
                return Unauthorized(new { Success = false, Message = "Refresh token lipsă în cookies." });
            }

            // 2. Apelăm serviciul trimițând direct string-ul
            var result = await _authService.RefreshTokenAsync(incomingRefreshToken);

            if (!result.Success)
                return Unauthorized(result);

            // 3. Dacă a funcționat, setăm noile cookie-uri
            var jwtCookieOptions = new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.None, Expires = DateTime.UtcNow.AddMinutes(5) };
            var refreshCookieOptions = new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.None, Expires = DateTime.UtcNow.AddDays(7) };

            Response.Cookies.Append("jwt", result.Token, jwtCookieOptions);
            Response.Cookies.Append("refreshToken", result.RefreshToken, refreshCookieOptions);

            return Ok(new { Success = true, Message = "Token reînnoit!", Role = result.Role, FullName = result.FullName });
        }
    }
}
