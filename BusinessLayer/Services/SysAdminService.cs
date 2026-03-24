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
    public class SysAdminService : ISysAdminService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        public SysAdminService(AppDbContext context, UserManager<AppUser> userManager)
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

        public async Task<bool> CreateInstitutionAdminAsync(CreateInstitutionDto dto)
        {
            var institution = new Institution
            {
                Id = Guid.NewGuid(),
                Name = dto.InstitutionName,
                CUI = dto.CUI,
                Address = dto.Address
            };
            await _context.Institutions.AddAsync(institution);
            await _context.SaveChangesAsync();

            var adminUser = new AppUser
            {
                UserName = dto.AdminEmail,
                Email = dto.AdminEmail,
                FullName = dto.AdminFullName,
                InstitutionId = institution.Id,
                Role=UserRole.InstitutionAdmin
            };

            var result = await _userManager.CreateAsync(adminUser, dto.AdminPassword);
            if (!result.Succeeded) { 
            
             _context.Institutions.Remove(institution);
                await _context.SaveChangesAsync();
                throw new Exception("Failed to create institution admin user.");
            }

            return true;


        }

        public async Task<bool> DeleteInstitutionAdminAsync(string email)
        {
            var admin = await _userManager.FindByEmailAsync(email);

            if (admin == null)
                throw new Exception("Administratorul nu a fost găsit.");

            if (admin.Role != UserRole.InstitutionAdmin)
                throw new Exception("Acest utilizator nu este un administrator de instituție.");

            // UserManager se ocupă automat de ștergerea în siguranță din baza de date
            var result = await _userManager.DeleteAsync(admin);

            return result.Succeeded;
        }

        public async Task<bool> DeleteInstitutionAsync(Guid institutionId)
        {
            var institution = await _context.Institutions.FindAsync(institutionId);
            if (institution == null) return false;

            var usersToDelete = await _userManager.Users.Where(u=>u.InstitutionId==institutionId).ToListAsync();
            
            foreach(var user in usersToDelete)
            {
                await _userManager.DeleteAsync(user);
            }
            _context.Institutions.Remove(institution);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<InstitutionDto>> GetAllInstitutionAsync()
        {
            // Acum pornim de la Instituții, nu de la Useri! 
            // Astfel, chiar dacă o instituție nu are admin, ea tot va apărea în listă.
            var institutions = await _context.Institutions.ToListAsync();
            var admins = await _userManager.Users.Where(u => u.Role == UserRole.InstitutionAdmin).ToListAsync();

            var result = institutions.Select(inst => {
                // Căutăm dacă există un admin legat de această instituție
                var admin = admins.FirstOrDefault(a => a.InstitutionId == inst.Id);

                return new InstitutionDto
                {
                    InstitutionId = inst.Id,
                    InstitutionName = inst.Name,
                    Cui = inst.CUI,
                    Address = inst.Address,
                    // Dacă nu există admin, Email și FullName vor fi null
                    Email = admin?.Email,
                    FullName = admin?.FullName
                };
            }).ToList();

            return result;
        } 

        public async Task<bool> ToggleDocumentTypeStatusAsync(Guid id)
        {
            var docType = await _context.DocumentTypes.FindAsync(id);
            if (docType == null) return false;

            docType.isActive = !docType.isActive;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateInstitutionAdminAsync(string email, UpdateInstitutionAdminDto dto)
        {
            var admin = await _userManager.FindByEmailAsync(email);

            if (admin == null)
                throw new Exception("Administratorul nu a fost găsit.");

            if (admin.Role != UserRole.InstitutionAdmin)
                throw new Exception("Acest utilizator nu este un administrator de instituție.");

            // Luăm noile date din DTO
            admin.FullName = dto.FullName;

            var result = await _userManager.UpdateAsync(admin);

            return result.Succeeded;
        }

        public async Task<bool> UpdateInstitutionAsync(Guid id, EditInstitutionDto dto)
        {
            var institution = await _context.Institutions.FindAsync(id);
            if (institution == null) return false;

            institution.Name = dto.InstitutionName;
            institution.CUI = dto.Cui;
            institution.Address = dto.Address;
            _context.Institutions.Update(institution);
            await _context.SaveChangesAsync();

            return true;
        }


        public async Task<List<DocumentType>> GetAllDocumentTypesAsync()
        {
              return await _context.DocumentTypes.OrderByDescending(d => d.EffectiveDate).ToListAsync();
        }

        public async Task<bool> AssignNewAdminAsync(CreateNewAdminDto dto)
        {
            var institution = await _context.Institutions.FindAsync(dto.InstitutionId);
            if (institution == null) throw new Exception("Institutia nu exista in sistem");

            var existingAdmin = await _userManager.Users.FirstOrDefaultAsync(
                u=>u.InstitutionId==dto.InstitutionId && u.Role==UserRole.InstitutionAdmin);

            var adminUser = new AppUser
            {
                UserName = dto.Email,
                Email=dto.Email,
                FullName=dto.FullName,
                InstitutionId=dto.InstitutionId,
                Role=UserRole.InstitutionAdmin
            };
            var result = await _userManager.CreateAsync(adminUser, dto.Password);
            if (!result.Succeeded) throw new Exception(" Eroare la crearea contului");

            return true;
        }
    }
}
