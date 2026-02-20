using BusinessLayer.DTOs;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLayer.Interfaces
{
    public interface IAuthService
    {
            Task<AuthResult> RegisterCitizenAsync(RegisterCitizenDto registerDto);
            
            Task<AuthResult> LoginAsync(LoginDto loginDto);
            Task<AuthResult> RefreshTokenAsync(TokenRequestDto tokenRequestDto);
          
    }
}
