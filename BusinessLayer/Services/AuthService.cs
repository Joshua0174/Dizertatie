using BusinessLayer.DTOs;
using BusinessLayer.Interfaces;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BusinessLayer.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(UserManager<AppUser> userManager, AppDbContext context, IConfiguration configuration)
        {
            _userManager = userManager;
            _context = context;
            _configuration = configuration;
        }

        //public async Task<AuthResult> CreateOfficerAsync(CreateOfficerDto createOfficerDto)
        //{
        //    //var existingUser = _userManager.FindByEmailAsync(createOfficerDto.Email).Result;

        //    //if (existingUser != null)
        //    //{
        //    //    return new AuthResult
        //    //    {
        //    //        Success = false,
        //    //        Errors = new List<string> { "Email already in use" }
        //    //    };
        //    //}

        //    //var user = new AppUser
        //    //{
        //    //    Email = createOfficerDto.Email,
        //    //    UserName = createOfficerDto.Email,
        //    //    FullName = createOfficerDto.FullName,
        //    //    Role = UserRole.Official
        //    //};


        //    //var result = await _userManager.CreateAsync(user, createOfficerDto.Password);
        //    //if (!result.Succeeded)
        //    //    return new AuthResult
        //    //    {
        //    //        Success = false,
        //    //        Errors = result.Errors.Select(e => e.Description).ToList()
        //    //    };
        //    //var officialProfile = new OfficialProfile
        //    //{
        //    //    UserId = user.Id,
        //    //    Institution = createOfficerDto.Institution,
        //    //    Department = createOfficerDto.Department
        //    //};
        //    //await _context.OfficialProfiles.AddAsync(officialProfile);
        //    //await _context.SaveChangesAsync();
        //    return new AuthResult
        //    {
        //        Success = true,
        //        Token = "Account Created Succesfully" // Nu generăm token la crearea oficialului, doar la login
        //    };
        //}

        public async Task<AuthResult> LoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password))
            {
                return new AuthResult
                {

                    Success = true,
                    Errors = new List<string> { " Email sau parola invalida" }
                };

            }

            return await GenerateJwtToken(user);
        }

        public async Task<AuthResult> RefreshTokenAsync(TokenRequestDto tokenRequestDto)
        {
            // 1. Căutăm Refresh Token-ul în baza de date
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == tokenRequestDto.RefreshToken);

            // 2. Verificăm dacă există
            if (storedToken == null)
            {
                return new AuthResult
                {
                    Success = false,
                    Errors = new List<string> { "Refresh token does not exist" }
                };
            }

            // 3. Verificăm data de expirare (IMPORTANT: Lipsea înainte)
            if (storedToken.ExpiryDate < DateTime.UtcNow)
            {
                return new AuthResult
                {
                    Success = false,
                    Errors = new List<string> { "Refresh token has expired. Please login again." }
                };
            }

            // 4. Verificăm dacă a fost deja folosit (Security: Replay Attack)
            if (storedToken.IsUsed)
            {
                return new AuthResult
                {
                    Success = false,
                    Errors = new List<string> { "Refresh token already used" }
                };
            }

            // 5. Verificăm dacă a fost revocat manual (Security: Ban)
            if (storedToken.IsRevoked)
            {
                return new AuthResult
                {
                    Success = false,
                    Errors = new List<string> { "Refresh token is revoked" }
                };
            }

            // 6. Marcăm token-ul ca fiind folosit (Invalidăm vechiul token)
            storedToken.IsUsed = true;
            _context.RefreshTokens.Update(storedToken);
            await _context.SaveChangesAsync();

            // 7. Găsim userul asociat acestui token
            var dbUser = await _userManager.FindByIdAsync(storedToken.UserId);

            if (dbUser == null)
            {
                return new AuthResult
                {
                    Success = false,
                    Errors = new List<string> { "User not found" }
                };
            }

            // 8. Generăm o pereche NOUĂ (Access Token + Refresh Token)
            return await GenerateJwtToken(dbUser);
        }

        public async Task<AuthResult> RegisterCitizenAsync(RegisterCitizenDto registerDto)
        {
            var existingUser = _userManager.FindByEmailAsync(registerDto.Email).Result;
            if (existingUser != null)
                return new AuthResult
                {
                    Success = false,
                    Errors = new List<string> { "Email already in use" }
                };

            var user = new AppUser
            {
                Email = registerDto.Email,
                UserName = registerDto.Email,
                FullName = registerDto.FullName,
                Role = UserRole.Citizen

            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
            {
                return new AuthResult
                {
                    Success = false,
                    Errors = result.Errors.Select(e => e.Description).ToList()
                };
            }
            var citizenProfile = new CitizenProfile
            {
                UserId = user.Id,
                CNP = registerDto.CNP
            };
            await _context.CitizenProfiles.AddAsync(citizenProfile);
            await _context.SaveChangesAsync();
            return await GenerateJwtToken(user);

        }

        private async Task<AuthResult> GenerateJwtToken(AppUser user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();

            // 1. Validare Cheie Secretă (Safety Check)
            var secretKey = _configuration.GetSection("JwtConfig:Secret").Value;
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new Exception("JWT Secret key is missing in appsettings.json");
            }

            var key = Encoding.UTF8.GetBytes(secretKey);

            // 2. Generăm ID-ul token-ului AICI pentru a-l avea salvat
            var tokenDescriptorId = Guid.NewGuid().ToString();

            // 3. Definim ce informații punem în token (Claims)
            var claims = new List<Claim>
    {
        new Claim("Id", user.Id),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim(JwtRegisteredClaimNames.Sub, user.Email),
        new Claim(JwtRegisteredClaimNames.Jti, tokenDescriptorId), // Folosim ID-ul generat sus
        new Claim("Role", user.Role.ToString())
    };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(5), // Token de acces scurt (5 min)
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = jwtTokenHandler.WriteToken(token);

            // 4. Creăm Refresh Token-ul
            var refreshToken = new RefreshToken
            {
                JwtId = tokenDescriptorId, // Legătură directă cu JWT-ul prin ID-ul generat manual
                IsUsed = false,
                IsRevoked = false,
                UserId = user.Id,
                AddedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddMonths(6), // Refresh token valabil 6 luni
                Token = Guid.NewGuid().ToString() // Token random simplu (metoda aleasă de tine)
            };

            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();

            return new AuthResult
            {
                Success = true,
                Token = jwtToken,
                RefreshToken = refreshToken.Token
            };
        }


    }
}
