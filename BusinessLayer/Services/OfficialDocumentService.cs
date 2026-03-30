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

        public async Task<PagedResult<DocumentRequestResponseDto>> GetPagedMyRequestsAsync(string officialUserId, int pageNumber, int pageSize, bool todayOnly = false)
        {
            var query = _context.DocumentRequests
                .Include(r => r.Citizen)
                .Include(r => r.DocumentType)
                .Where(r => r.OfficialId == officialUserId)
                .AsNoTracking(); // OPTIMIZARE: AsNoTracking crește performanța pentru operațiunile de tip Read-Only

            // 1. APLICĂ FILTRUL PENTRU ZIUA CURENTĂ (dacă a fost cerut din frontend)
            if (todayOnly)
            {
                var todayStart = DateTime.UtcNow.Date; // Ora 00:00:00 UTC a zilei curente
                query = query.Where(r => r.RequestDate >= todayStart);
            }

            // 2. CALCULEAZĂ TOTALUL (După ce am aplicat filtrul, ca să bată numărul de pagini cu realitatea)
            var totalCount = await query.CountAsync();

            // 3. EXTRAGE DATELE PAGINATE
            var items = await query
                .OrderByDescending(r => r.RequestDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new DocumentRequestResponseDto
                {
                    Id = r.Id,
                    // AICI ESTE CORECTURA: Fără ".User", pentru că r.Citizen ESTE AppUser direct
                    CitizenName = r.Citizen.FullName,
                    CitizenEmail = r.Citizen.Email,

                    DocumentName = r.DocumentType.Name,
                    Status = r.Status.ToString(),
                    Date = r.RequestDate,
                    RequestReason = r.RequestReason,
                    RejectionReason = r.RejectionReason,
                    ResponseDate = r.ResponseDate,
                    DocumentTypeId = r.DocumentTypeId
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

        // ==========================================
        // 1. METODĂ NOUĂ PENTRU STATISTICI
        // ==========================================
        public async Task<OfficialStatsDto> GetOfficialStatsAsync(string officialUserId)
        {
            var query = _context.DocumentRequests.Where(r => r.OfficialId == officialUserId).AsNoTracking();

            var timeLimit = DateTime.UtcNow.AddMinutes(-5);
            var todayStart = DateTime.UtcNow.Date; // De la miezul nopții UTC
            var queryToday = query.Where(r => r.RequestDate >= todayStart);

            return new OfficialStatsDto
            {
                AllTime = new StatMetrics
                {
                    Total = await query.CountAsync(),
                    Pending = await query.CountAsync(r => r.Status == RequestStatus.Pending),
                    // NOU: Am adăugat verificarea pentru RequestStatus.Resolved
                    Resolved = await query.CountAsync(r =>
                        r.Status == RequestStatus.Rejected ||
                        r.Status == RequestStatus.Resolved ||
                        (r.Status == RequestStatus.Approved && r.ResponseDate != null && r.ResponseDate >= timeLimit)),
                    Expired = await query.CountAsync(r =>
                        r.Status == RequestStatus.Expired ||
                        (r.Status == RequestStatus.Approved && r.ResponseDate != null && r.ResponseDate < timeLimit))
                },
                Today = new StatMetrics
                {
                    Total = await queryToday.CountAsync(),
                    Pending = await queryToday.CountAsync(r => r.Status == RequestStatus.Pending),
                    // NOU: Am adăugat verificarea pentru RequestStatus.Resolved
                    Resolved = await queryToday.CountAsync(r =>
                        r.Status == RequestStatus.Rejected ||
                        r.Status == RequestStatus.Resolved ||
                        (r.Status == RequestStatus.Approved && r.ResponseDate != null && r.ResponseDate >= timeLimit)),
                    Expired = await queryToday.CountAsync(r =>
                        r.Status == RequestStatus.Expired ||
                        (r.Status == RequestStatus.Approved && r.ResponseDate != null && r.ResponseDate < timeLimit))
                }
            };
        }

        // ==========================================
        // 2. METODĂ NOUĂ PENTRU ISTORIC DOSAR
        // ==========================================
        public async Task<List<CitizenHistoryDto>> GetCitizenHistoryAsync(string officialUserId, string cnp)
        {
            var citizenProfile = await _context.CitizenProfiles.FirstOrDefaultAsync(cp => cp.CNP == cnp);
            if (citizenProfile == null)
                throw new KeyNotFoundException("Cetățeanul nu a fost găsit.");

            // Aduce istoricul cererilor făcute de ACEST funcționar (sau de instituția lui, în funcție de regula de business. Aici am lăsat doar cererile lui).
            var history = await _context.DocumentRequests
                .Include(r => r.DocumentType)
                .Where(r => r.CitizenId == citizenProfile.UserId && r.OfficialId == officialUserId)
                .OrderByDescending(r => r.RequestDate)
                .AsNoTracking()
                .Select(r => new CitizenHistoryDto
                {
                    Id = r.Id,
                    DocumentName = r.DocumentType.Name,
                    Status = r.Status.ToString(),
                    Date = r.RequestDate,
                    RejectionReason = r.RejectionReason
                })
                .ToListAsync();

            return history;
        }

        public async Task<bool> ResolveRequestAsync(Guid requestId, string officialUserId)
        {
            var request = await _context.DocumentRequests
                .FirstOrDefaultAsync(r => r.Id == requestId && r.OfficialId == officialUserId);

            if (request == null) return false;

            // Doar cererile 'Approved' care nu au expirat încă pot fi soluționate
            if (request.Status != RequestStatus.Approved) return false;

            request.Status = RequestStatus.Resolved;

            // IMPORTANT: Ștergem calea către fișier pentru a bloca accesul definitiv după vizualizare
            request.DocumentPath = null;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}