using BusinessLayer.DTOs;
using BusinessLayer.Helpers;
using BusinessLayer.Interfaces;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public class OfficialDocumentService : IOfficialDocumentService
    {
        private readonly AppDbContext _context;

        // Am eliminat UserManager dacă nu îl folosești direct aici, ca să păstrăm constructorul curat.
        public OfficialDocumentService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<DocumentRequestResponseDto>> GetPagedMyRequestsAsync(string officialUserId, int pageNumber, int pageSize)
        {
            var query = _context.DocumentRequests
                .Include(r => r.Citizen)
                .Include(r => r.DocumentType)
                .Where(r => r.OfficialId == officialUserId)
                .AsNoTracking(); // OPTIMIZARE: AsNoTracking crește performanța pentru operațiunile de tip Read-Only

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(r => r.RequestDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new DocumentRequestResponseDto
                {
                    Id = r.Id,
                    CitizenEmail = r.Citizen.Email,
                    DocumentName = r.DocumentType.Name,
                    Status = r.Status.ToString(),
                    Date = r.RequestDate,
                    RequestReason = r.RequestReason,
                    RejectionReason = r.RejectionReason,
                    ResponseDate = r.ResponseDate
                })
                .ToListAsync();

            return new PagedResult<DocumentRequestResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<DocumentRequest> SendRequestAsync(CreateDocumentRequestDto dto, string officialUserId)
        {
            var citizenProfile = await _context.CitizenProfiles.FirstOrDefaultAsync(cp => cp.CNP == dto.CitizenCnp);
            if (citizenProfile == null)
                throw new KeyNotFoundException("Nu exista niciun cetatean cu acest CNP."); // Folosim KeyNotFound pentru 404

            var officialProfile = await _context.OfficialProfiles.FirstOrDefaultAsync(o => o.UserId == officialUserId);
            if (officialProfile == null)
                throw new KeyNotFoundException("Nu exista niciun functionar cu acest cont.");

            // --- SECURITATE (RBAC) ---
            var allowedDocs = await GetAllowedDocumentTypesAsync(officialUserId);
            if (!allowedDocs.Any(d => d.Id == dto.DocumentTypeId))
            {
                throw new UnauthorizedAccessException("Securitate: Nu ai permisiunea de a cere acest tip de document!"); // 403 Forbidden
            }

            // ====================================================================
            // --- NOU: LOGICA DE FAIL-FAST (AUTO-REJECT) ---
            // Verificăm dacă cetățeanul chiar are acest document în contul său
            // ====================================================================
            bool hasDocument = await _context.CitizenDocuments
                .AnyAsync(d => d.UserId == citizenProfile.UserId && d.DocumentTypeId == dto.DocumentTypeId);

            var request = new DocumentRequest
            {
                Id = Guid.NewGuid(),
                OfficialId = officialUserId,
                CitizenId = citizenProfile.UserId,
                DocumentTypeId = dto.DocumentTypeId,
                RequestDate = DateTime.UtcNow,
                RequestReason = dto.Reason,

                // Dacă are documentul e Pending. Dacă NU îl are, e direct Rejected!
                Status = hasDocument ? RequestStatus.Pending : RequestStatus.Rejected,

                // Completăm motivul automat dacă a picat testul
                RejectionReason = hasDocument ? null : "Sistem auto-reject: Cetățeanul nu deține acest document în portofelul digital."
            };

            await _context.DocumentRequests.AddAsync(request);
            await _context.SaveChangesAsync();

            return request;
        }

        public async Task<List<DocumentTypeDto>> GetAllowedDocumentTypesAsync(string officialUserId)
        {
            var officialProfile = await _context.OfficialProfiles
                .AsNoTracking() // Din nou, Read-Only, nu modificăm datele aici
                .Include(p => p.CompetencyProfile)
                    .ThenInclude(cp => cp.AllowedDocumentTypes)
                    .ThenInclude(pdt => pdt.DocumentType)
                .FirstOrDefaultAsync(p => p.UserId == officialUserId);

            if (officialProfile?.CompetencyProfile == null)
            {
                return new List<DocumentTypeDto>();
            }

            return officialProfile.CompetencyProfile.AllowedDocumentTypes
                .Where(pdt => pdt.DocumentType.isActive)
                .Select(pdt => new DocumentTypeDto
                {
                    Id = pdt.DocumentType.Id,
                    Name = pdt.DocumentType.Name,
                    Description = pdt.DocumentType.Description
                })
                .ToList();
        }

        // Returnăm DTO-ul, nu `object`
        public async Task<CitizenSearchResponseDto> SearchCitizenByCnpAsync(string cnp)
        {
            var citizenProfile = await _context.CitizenProfiles
                .AsNoTracking()
                .Include(cp => cp.User)
                .FirstOrDefaultAsync(cp => cp.CNP == cnp);

            if (citizenProfile == null)
                throw new KeyNotFoundException("Nu am găsit niciun cetățean cu acest CNP.");

            return new CitizenSearchResponseDto
            {
                Id = citizenProfile.UserId,
                Email = citizenProfile.User.Email,
                FullName = citizenProfile.User.FullName,
                Cnp = citizenProfile.CNP
            };
        }

        public async Task<(byte[] FileBytes, string FileName)> DownloadRequestedDocumentAsync(Guid requestId, string officialUserId)
        {
            var request = await _context.DocumentRequests
                .Include(r => r.DocumentType)
                .Include(r => r.Citizen)
                .FirstOrDefaultAsync(r => r.Id == requestId && r.OfficialId == officialUserId);

            if (request == null)
                throw new KeyNotFoundException("Cererea nu există sau nu îți aparține.");

            if (request.Status != RequestStatus.Approved || string.IsNullOrEmpty(request.DocumentPath))
                throw new InvalidOperationException("Documentul nu este disponibil pentru descărcare."); // 400 Bad Request

            if (request.ResponseDate.HasValue)
            {
                var timePassed = DateTime.UtcNow - request.ResponseDate.Value;
                if (timePassed.TotalMinutes > 5)
                {
                    request.Status = RequestStatus.Expired;
                    await _context.SaveChangesAsync();

                    throw new InvalidOperationException("Securitate: Timpul alocat (5 minute) a expirat. Fă o nouă cerere.");
                }
            }

            if (!File.Exists(request.DocumentPath))
                throw new KeyNotFoundException("Fișierul fizic lipsește de pe server.");

            var encryptedBytes = await File.ReadAllBytesAsync(request.DocumentPath);

            byte[] decryptedPdfBytes;
            try
            {
                decryptedPdfBytes = EncryptionHelper.Decrypt(encryptedBytes);
            }
            catch (Exception)
            {
                throw new InvalidOperationException("Eroare de securitate: Documentul nu a putut fi decriptat.");
            }

            string fileName = $"{request.DocumentType.Name}_{request.Citizen.Email}.pdf";
            return (decryptedPdfBytes, fileName);
        }
    }
}