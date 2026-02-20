using BusinessLayer.DTOs;
using BusinessLayer.Interfaces;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
        public AdminService(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context; 
            _userManager = userManager;
        }
        public async Task<DocumentType> CreateDocumentTypeAsync(CreateDocumentTypeDto dto)
        {
            var newType = new DocumentType
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                EffectiveDate = dto.EffectiveDate,
                ExpirationDate = dto.ExpirationDate,
                isActive = true
            };

            await _context.DocumentTypes.AddAsync(newType);
            await _context.SaveChangesAsync();
            return newType;

        }

        public async Task<List<DocumentType>> GetAllDocumentTypesAsync()
        {
           return await _context.DocumentTypes.OrderBy(t => t.Name).ToListAsync();
        }

        public async Task<List<OfficialProfile>> GetAllOfficialAsync()
        {
              return await _context.OfficialProfiles.Include(o => o.User)
                                                    .Include(o => o.CompetencyProfile)
                                                    .ToListAsync();
        }

        public async Task<AppUser> RegisterOfficialAsync(CreateOfficerDto dto)
        {
            // 1. Creăm userul
            var user = new AppUser
            {
                Email = dto.Email,
                UserName = dto.Email,
                FullName = dto.FullName,

                // IMPORTANT: Aici setăm rolul direct!
                Role = UserRole.Official
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
                Institution = dto.Institution,
                CompetencyProfileId = dto.CompetencyProfileId,
                EmployeeCode = dto.EmployeeCode
            };

            await _context.OfficialProfiles.AddAsync(officialProfile);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<bool> ToggleDocumentTypeStatusAsync(Guid Id)
        {
            var docType = await _context.DocumentTypes.FindAsync(Id);
            if (docType == null) return false;
            
            docType.isActive = !docType.isActive;
            await _context.SaveChangesAsync();

            return true;
        } 
    }
}
