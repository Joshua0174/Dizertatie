using BusinessLayer.DTOs;
using BusinessLayer.Helpers;
using BusinessLayer.Interfaces;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace BusinessLayer.Services
{
    public class CitizenDocumentService : ICitizenDocumentService
    {
        private readonly AppDbContext _context;

        public CitizenDocumentService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<CitizenDocument>> GetUserDocumentsAsync(string userId)
        {
            return await _context.CitizenDocuments
                .AsNoTracking()
                .Include(d => d.DocumentType)
                .Where(d => d.UserId == userId)
                .ToListAsync();
        }

        public async Task<CitizenDocument> GetDocumentByIdAsync(Guid documentId, string userId)
        {
            return await _context.CitizenDocuments
                .AsNoTracking()
                .Include(d => d.DocumentType)
                .FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId);
        }

        public async Task<CitizenDocument> UploadDocumentAsync(string userId, IFormFile file, string documentName, Guid documentTypeId)
        {
            // 1. Validări de bază
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId), "UserId invalid");
            if (file == null || file.Length == 0) throw new ArgumentNullException(nameof(file), "Fișier gol");

            // Verificăm dacă tipul de document ales chiar există
            var typeExists = await _context.DocumentTypes.AnyAsync(t => t.Id == documentTypeId);
            if (!typeExists) throw new ArgumentException("Tipul de document selectat nu este valid.");

            // 2. CONVERSIE: Procesăm fișierul prin Helper-ul de PDF
            byte[] pdfBytes;
            try
            {
                pdfBytes = await PdfConversionHelper.ConvertToPdfAsync(file);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Eroare la procesarea fișierului: {ex.Message}");
            }

            // 3. HASH: Calculăm amprenta digitală SHA256 (Esențial pentru Blockchain) pe PDF-ul curat
            string hash;
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(pdfBytes);
                hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }

            // =====================================================================
            // 4. LOGICA DE UPSERT: Căutăm dacă omul are deja acest tip de act
            // =====================================================================
            var existingDocument = await _context.CitizenDocuments
                .FirstOrDefaultAsync(d => d.UserId == userId && d.DocumentTypeId == documentTypeId);

            // Dacă există, îi refolosim ID-ul (pentru a suprascrie fișierul fizic). Altfel, generăm unul nou.
            var documentId = existingDocument != null ? existingDocument.Id : Guid.NewGuid();

            // 5. Pregătire stocare fizică
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", userId);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Criptăm conținutul fișierului PDF cu AES-256
            var encryptedBytes = EncryptionHelper.Encrypt(pdfBytes);

            // Salvăm cu extensia .enc
            var uniqueFileName = $"{documentId}.enc";
            var fullPath = Path.Combine(folderPath, uniqueFileName);

            // Dacă calea veche diferă de calea nouă din anumite motive, ștergem vechiul fișier.
            // (De regulă va fi aceeași și WriteAllBytesAsync va face suprascriere automată)
            if (existingDocument != null && !string.IsNullOrEmpty(existingDocument.FilePath) && existingDocument.FilePath != fullPath)
            {
                if (File.Exists(existingDocument.FilePath))
                {
                    File.Delete(existingDocument.FilePath);
                }
            }

            // Scriem pe disk varianta CRIPTATĂ (dacă există deja fișierul, îl va suprascrie curat)
            await File.WriteAllBytesAsync(fullPath, encryptedBytes);

            // =====================================================================
            // 6. ACTUALIZARE SAU SALVARE ÎN BAZA DE DATE
            // =====================================================================
            if (existingDocument != null)
            {
                // Update pe cel existent
                existingDocument.Name = documentName;
                existingDocument.FilePath = fullPath;
                existingDocument.FileHash = hash;
                existingDocument.FileType = "application/pdf";
                existingDocument.UploadedDate = DateTime.UtcNow;
                existingDocument.IsOnBlockChain = false; // Resetăm pt. că actul s-a schimbat!

                _context.CitizenDocuments.Update(existingDocument);
                await _context.SaveChangesAsync();

                return existingDocument;
            }
            else
            {
                // Insert pentru act nou
                var document = new CitizenDocument
                {
                    Id = documentId,
                    UserId = userId,
                    DocumentTypeId = documentTypeId,
                    Name = documentName,
                    FilePath = fullPath,
                    FileHash = hash,
                    FileType = "application/pdf",
                    UploadedDate = DateTime.UtcNow,
                    IsOnBlockChain = false
                };

                await _context.CitizenDocuments.AddAsync(document);
                await _context.SaveChangesAsync();

                return document;
            }
        }

        // ==============================================================================
        // METODE NOI PENTRU COMUNICAREA CU FUNCȚIONARUL
        // ==============================================================================

        public async Task<PagedResult<DocumentRequestResponseDto>> GetPagedMyRequestsAsync(string citizenUserId, int pageNumber, int pageSize)
        {
            var query = _context.DocumentRequests
                .AsNoTracking() // Optimizare pentru read-only
                .Include(r => r.Official)
                .Include(r => r.DocumentType)
                .Where(r => r.CitizenId == citizenUserId)
                .AsQueryable();

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(r => r.RequestDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new DocumentRequestResponseDto
                {
                    Id = r.Id,
                    CitizenEmail = r.Official.Email,
                    DocumentName = r.DocumentType.Name,
                    Status = r.Status.ToString(),
                    Date = r.RequestDate,
                    RequestReason = r.RequestReason,
                    RejectionReason = r.RejectionReason
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

        public async Task<DocumentRequest> RespondToRequestAsync(RespondToRequestDto dto, string citizenUserId)
        {
            // Am inclus DocumentType pentru a putea afișa numele documentului în mesajul de eroare
            var request = await _context.DocumentRequests
                .Include(r => r.DocumentType)
                .FirstOrDefaultAsync(r => r.Id == dto.RequestId && r.CitizenId == citizenUserId);

            if (request == null) throw new KeyNotFoundException("Cererea nu a fost găsită sau nu îți aparține.");
            if (request.Status != RequestStatus.Pending) throw new InvalidOperationException("Această cerere a primit deja un răspuns.");

            request.ResponseDate = DateTime.UtcNow; // Începe să ticăie cronometrul de 5 minute

            if (dto.IsApproved)
            {
                // 1. Căutăm documentul în portofelul cetățeanului
                var existingDocument = await _context.CitizenDocuments
                    .FirstOrDefaultAsync(d => d.UserId == citizenUserId && d.DocumentTypeId == request.DocumentTypeId);

                if (existingDocument == null)
                {
                    throw new InvalidOperationException($"Nu ai un document valid de tipul '{request.DocumentType.Name}' încărcat în cont. Te rugăm să îl încarci mai întâi în portofelul tău de documente.");
                }

                // 2. Legăm cererea de fișierul deja existent și criptat
                request.DocumentPath = existingDocument.FilePath;
                request.Status = RequestStatus.Approved;
            }
            else
            {
                // Fluxul de respingere
                if (string.IsNullOrWhiteSpace(dto.RejectionReason))
                    throw new ArgumentException("Trebuie să oferi un motiv pentru refuz.");

                request.RejectionReason = dto.RejectionReason;
                request.Status = RequestStatus.Rejected;
            }

            await _context.SaveChangesAsync();
            return request;
        }
        public async Task<CitizenStatsDto> GetCitizenStatsAsync(string citizenId)
        {
            // Filtrăm doar cererile acestui cetățean
            var query = _context.DocumentRequests.Where(r => r.CitizenId == citizenId);

            // Numărăm direct în baza de date
            var total = await query.CountAsync();
            var pending = await query.CountAsync(r => r.Status == RequestStatus.Pending);

            // Soluționate înseamnă orice nu e Pending (Aprobate, Respinse, Expirate)
            var resolved = total - pending;

            return new CitizenStatsDto
            {
                TotalRequests = total,
                PendingRequests = pending,
                ResolvedRequests = resolved
            };
        }
    }
}