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
    public class OfficialDocumentService : IOfficialDocumentService
    {   
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        public OfficialDocumentService(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        public async Task<List<DocumentRequest>> GetMyRequestAsync(string officialUserId)
        {
            return await _context.DocumentRequests
                .Include(r => r.Citizen)
                .Include(r => r.DocumentType)
                .Where(r => r.OfficialId == officialUserId)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();
        }

        public async Task<DocumentRequest> SendRequestAsync(CreateDocumentRequestDto dto, string officialUserId)
        {
            var citizen = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == dto.CitizenEmail); 
            if (citizen == null) throw new Exception("Nu exista niciun cetatean cu acest email.");

            var officialProfile=await _context.OfficialProfiles.FirstOrDefaultAsync(o => o.UserId==officialUserId);
            if (officialProfile == null) throw new Exception("Nu exista niciun functionar cu acest userId."); 


            var request = new DocumentRequest
            {
                Id = Guid.NewGuid(),
                OfficialId = officialUserId,
                CitizenId = citizen.Id,
                DocumentTypeId = dto.DocumentTypeId,
                Status = RequestStatus.Pending,
                RequestDate = DateTime.UtcNow,
                RejectionReason = dto.Reason
            };
            
            await _context.DocumentRequests.AddAsync(request);
            await _context.SaveChangesAsync();
            return request;
        }
    }
}
