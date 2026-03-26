using BusinessLayer.DTOs;
using BusinessLayer.Helpers;
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
        
        public async Task<PagedResult<DocumentRequestResponseDto>> GetPagedMyRequestsAsync(string officialUserId, int pageNumber, int pageSize)
        {
            // 1. Construim query-ul de bază (FĂRĂ să aducem datele din baza de date încă)
            var query = _context.DocumentRequests
                .Include(r => r.Citizen)
                .Include(r => r.DocumentType)
                .Where(r => r.OfficialId == officialUserId)
                .AsQueryable();

            // 2. Numărăm totalul (necesar pentru UI ca să știe câte pagini există)
            var totalCount = await query.CountAsync();

            // 3. Extragem doar pagina dorită
            var items = await query
                .OrderByDescending(r => r.RequestDate) // Cele mai noi cereri sus
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
                .ToListAsync(); // Aici se execută interogarea în SQL

            // 4. Împachetăm rezultatul în clasa noastră generică
            return new PagedResult<DocumentRequestResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        // 2. Trimitem cererea pe baza CNP-ului
        public async Task<DocumentRequest> SendRequestAsync(CreateDocumentRequestDto dto, string officialUserId)
        {
            // Căutăm profilul cetățeanului după CNP
            var citizenProfile = await _context.CitizenProfiles.FirstOrDefaultAsync(cp => cp.CNP == dto.CitizenCnp);
            if (citizenProfile == null) throw new Exception("Nu exista niciun cetatean cu acest CNP.");

            var officialProfile = await _context.OfficialProfiles.FirstOrDefaultAsync(o => o.UserId == officialUserId);
            if (officialProfile == null) throw new Exception("Nu exista niciun functionar cu acest cont.");

            // --- SECURITATE (RBAC) ---
            var allowedDocs = await GetAllowedDocumentTypesAsync(officialUserId);
            if (!allowedDocs.Any(d => d.Id == dto.DocumentTypeId))
            {
                throw new Exception("Securitate: Nu ai permisiunea de a cere acest tip de document!");
            }

            var request = new DocumentRequest
            {
                Id = Guid.NewGuid(),
                OfficialId = officialUserId,
                CitizenId = citizenProfile.UserId, // Aici punem UserId-ul găsit prin CNP
                DocumentTypeId = dto.DocumentTypeId,
                Status = RequestStatus.Pending,
                RequestDate = DateTime.UtcNow,
                RequestReason = dto.Reason,
                RejectionReason = null
            };

            await _context.DocumentRequests.AddAsync(request);
            await _context.SaveChangesAsync();
            return request;
        }
        public async Task<List<DocumentTypeDto>> GetAllowedDocumentTypesAsync(string officialUserId)
        {
            // 1. Căutăm profilul funcționarului logat și includem relațiile către documente
            var officialProfile = await _context.OfficialProfiles
                .Include(p => p.CompetencyProfile)
                    .ThenInclude(cp => cp.AllowedDocumentTypes) // Intrăm în tabelul de legătură
                    .ThenInclude(pdt => pdt.DocumentType)       // Aducem entitatea efectivă a documentului
                .FirstOrDefaultAsync(p => p.UserId == officialUserId);

            // 2. Dacă funcționarul nu are un profil setat, îi întoarcem o listă goală (nu are voie să ceară nimic)
            if (officialProfile == null || officialProfile.CompetencyProfile == null)
            {
                return new List<DocumentTypeDto>();
            }

            // 3. Extragem documentele, le filtrăm doar pe cele active și le mapăm în DTO-ul tău
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

        public async Task<object> SearchCitizenByCnpAsync(string cnp)
        {
            // Căutăm în profil, dar includem și clasa User ca să îi luăm numele/emailul
            var citizenProfile = await _context.CitizenProfiles
                .Include(cp => cp.User)
                .FirstOrDefaultAsync(cp => cp.CNP == cnp);

            if (citizenProfile == null) throw new Exception("Nu am găsit niciun cetățean cu acest CNP.");

            return new
            {
                Id = citizenProfile.UserId, // Funcționarul are nevoie de UserId pentru a trimite cererea
                Email = citizenProfile.User.Email,
                FullName = citizenProfile.User.FullName,
                Cnp = citizenProfile.CNP
            };
        }

        public async Task<(byte[] FileBytes, string FileName)> DownloadRequestedDocumentAsync(Guid requestId, string officialUserId)
        {
            // 1. Căutăm cererea și ne asigurăm că aparține acestui funcționar
            var request = await _context.DocumentRequests
                .Include(r => r.DocumentType)
                .Include(r => r.Citizen)
                .FirstOrDefaultAsync(r => r.Id == requestId && r.OfficialId == officialUserId);

            if (request == null) throw new Exception("Cererea nu există sau nu îți aparține.");

            if (request.Status != RequestStatus.Approved || string.IsNullOrEmpty(request.DocumentPath))
                throw new Exception("Documentul nu este disponibil pentru descărcare.");
            if (request.ResponseDate.HasValue)
            {
                var timePassed = DateTime.UtcNow - request.ResponseDate.Value;
                if (timePassed.TotalMinutes > 5)
                {
                    // Opțional: Poți chiar să schimbi statusul cererii în "Expired" aici ca să știe baza de date
                    request.Status = RequestStatus.Expired;
                    await _context.SaveChangesAsync();

                    throw new Exception("Securitate: Timpul alocat (5 minute) pentru descărcarea acestui document a expirat. Trebuie să faci o nouă cerere cetățeanului.");
                }
            }
            if (!File.Exists(request.DocumentPath))
                throw new Exception("Fișierul fizic lipsește de pe server. Posibil să fi fost șters.");

            // 2. Citim fișierul CRIPTAT de pe disc (.enc)
            var encryptedBytes = await File.ReadAllBytesAsync(request.DocumentPath);

            // 3. DECRIPTAREA FIȘIERULUI ÎN MEMORIE
            byte[] decryptedPdfBytes;
            try
            {
                decryptedPdfBytes = EncryptionHelper.Decrypt(encryptedBytes);
            }
            catch (Exception)
            {
                throw new Exception("Eroare de securitate: Documentul nu a putut fi decriptat.");
            }

            // 4. Formăm un nume drăguț pentru descărcare
            string fileName = $"{request.DocumentType.Name}_{request.Citizen.Email}.pdf";

            return (decryptedPdfBytes, fileName);
        }

    }
}
