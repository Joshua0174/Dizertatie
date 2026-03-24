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
       
        

        public async Task<List<OfficialProfileDto>> GetAllOfficialAsync()
        {
            var institutionId = GetCurrentInstitutionId();
            if (institutionId == null) return new List<OfficialProfileDto>();

            return await _context.OfficialProfiles
                .Include(o => o.User)
                .Include(o => o.CompetencyProfile)
                .Where(o => o.User.InstitutionId == institutionId)
                .Select(o => new OfficialProfileDto
                {
                    Id = o.Id,
                    FullName = o.User.FullName,
                    Email = o.User.Email,
                    EmployeeCode = o.EmployeeCode,
                    // Dacă funcționarul are profil, îi luăm numele, altfel punem N/A
                    CompetencyProfileName = o.CompetencyProfile != null ? o.CompetencyProfile.Name : "N/A"
                })
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

        public async Task<CompetencyProfile> CreateCompetencyProfileAsync(CreateCompetencyProfileDto dto)
        {
            var institutionId = GetCurrentInstitutionId();
            if (institutionId == null) throw new Exception("Nu poți crea un profil fără instituție.");

            // 1. Creăm profilul de bază
            var profile = new CompetencyProfile
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                InstitutionId = institutionId.Value
            };

            await _context.CompetencyProfiles.AddAsync(profile);

            // 2. AICI E MAGIA: Legăm profilul de documentele pe care are voie să le ceară
            if (dto.AllowedDocumentTypeIds != null && dto.AllowedDocumentTypeIds.Any())
            {
                foreach (var docTypeId in dto.AllowedDocumentTypeIds)
                {
                    var profileDoc = new ProfileDocumentType
                    {
                        CompetencyProfileId = profile.Id,
                        DocumentTypeId = docTypeId
                    };
                    await _context.ProfileDocumentTypes.AddAsync(profileDoc);
                }
            }

            await _context.SaveChangesAsync();
            return profile;
        }

        public async Task<List<CompetencyProfile>> GetCompetencyProfilesAsync()
        {
            var institutionId = GetCurrentInstitutionId();
            if (institutionId == null) return new List<CompetencyProfile>();

            return await _context.CompetencyProfiles
                .Where(p => p.InstitutionId == institutionId.Value)
                .ToListAsync();
        }

       
        public async Task<PagedResult<DocumentTypeDto>> GetPagedSystemDocumentTypesAsync(int pageNumber, int pageSize, string searchTerm)
        {
            // 1. Luăm doar query-ul, FĂRĂ să aducem datele din baza de date încă
            var query = _context.DocumentTypes.Where(d => d.isActive).AsQueryable();

            // 2. Aplicăm filtrul de căutare direct pe server (SQL LIKE)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(d => d.Name.ToLower().Contains(searchTerm) ||
                                         (d.Description != null && d.Description.ToLower().Contains(searchTerm)));
            }

            // 3. Numărăm câte rezultate există în TOTAL (necesar pentru frontend ca să deseneze butoanele de pagini)
            var totalCount = await query.CountAsync();

            // 4. Aducem STRICT pagina pe care o vrem
            var items = await query
                .OrderBy(d => d.Name) // Întotdeauna trebuie să ordonezi înainte de Skip/Take!
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new DocumentTypeDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Description = d.Description
                })
                .ToListAsync(); // Abia AICI se execută interogarea finală în baza de date!

            return new PagedResult<DocumentTypeDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}
