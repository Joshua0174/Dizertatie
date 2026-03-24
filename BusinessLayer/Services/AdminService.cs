using BusinessLayer.DTOs;
using BusinessLayer.Interfaces;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Services
{
    public class AdminService : IAdminService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager; 
        private readonly IHttpContextAccessor _httpContextAccessor;
        public AdminService(AppDbContext context, UserManager<AppUser> userManager, IHttpContextAccessor httpContextAccessor)
        {
            _context = context; 
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        private Guid? GetCurrentInstitutionId() { 
        
             var instIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("InstitutionId").Value;
            if(string.IsNullOrEmpty(instIdClaim)) return null;
            return Guid.Parse(instIdClaim);
        }
       
        

        public async Task<List<OfficialProfile>> GetAllOfficialAsync()
        {
              return await _context.OfficialProfiles.Include(o => o.User)
                                                    .Include(o => o.CompetencyProfile)
                                                    .Where(o => o.User.InstitutionId == GetCurrentInstitutionId())
                                                    .ToListAsync();
        }

        public async Task<AppUser> RegisterOfficialAsync(CreateOfficerDto dto)
        {  
            var institutionId = GetCurrentInstitutionId();
            if(institutionId == null) 
                throw new Exception("Nu poti crea un functionar pentru ca nu esti asociat unei institutii.");
            // 1. Creăm userul
            var user = new AppUser
            {
                Email = dto.Email,
                UserName = dto.Email,
                FullName = dto.FullName,

                // IMPORTANT: Aici setăm rolul direct!
                Role = UserRole.Official,
                InstitutionId=institutionId
            };

            // 2. Îl salvăm în baza de date
            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                var errrors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Failed to create user: {errrors}");
            }

            // --- ZONA CRITICĂ ---
            // AICI NU TREBUIE SĂ EXISTE NIMIC legat de AddToRoleAsync!
            // Dacă ai linia asta, ȘTERGE-O: await _userManager.AddToRoleAsync(user, "Official");
            // --------------------

            // 3. Creăm profilul
            var officialProfile = new OfficialProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CompetencyProfileId = dto.CompetencyProfileId,
                EmployeeCode = dto.EmployeeCode
            };

            await _context.OfficialProfiles.AddAsync(officialProfile);
            await _context.SaveChangesAsync();

            return user;
        }

        
    }
}
