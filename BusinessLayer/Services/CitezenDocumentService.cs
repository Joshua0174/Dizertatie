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
                .Include(d => d.DocumentType) // Recomandat: include și tipul pentru a-l afișa în UI
                .Where(d => d.UserId == userId)
                .ToListAsync();
        }

        public async Task<CitizenDocument> GetDocumentByIdAsync(Guid documentId, string userId)
        {
            return await _context.CitizenDocuments
                .Include(d => d.DocumentType)
                .FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId);
        }

        public async Task<CitizenDocument> UploadDocumentAsync(string userId, IFormFile file, string documentName, Guid documentTypeId)
        {
            // 1. Validări de bază
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId), "UserId invalid");
            if (file == null || file.Length == 0) throw new ArgumentNullException(nameof(file), "Fișier gol");

            // Verificăm dacă tipul de document ales chiar există în nomenclator
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

            // 3. HASH: Calculăm amprenta digitală SHA256 (Esențial pentru Blockchain)
            // Calculăm hash-ul pe PDF-ul ORIGINAL, CURAT.
            string hash;
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(pdfBytes);
                hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }

            // 4. Pregătire stocare fizică
            var newDocumentId = Guid.NewGuid();
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", userId);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // =======================================================
            // 5. CRIPTAREA ȘI SALVAREA PE DISK (Securitate maximă)
            // =======================================================

            // Criptăm conținutul fișierului PDF cu AES-256
            var encryptedBytes = EncryptionHelper.Encrypt(pdfBytes);

            // Salvăm cu extensia .enc pentru a indica vizual că fișierul este sigilat criptografic
            var uniqueFileName = $"{newDocumentId}.enc";
            var fullPath = Path.Combine(folderPath, uniqueFileName);

            // Scriem pe disk varianta CRIPTATĂ (care e complet ilizibilă fără cheie)
            await File.WriteAllBytesAsync(fullPath, encryptedBytes);

            // =======================================================

            // 6. SALVARE ÎN BAZA DE DATE
            var document = new CitizenDocument
            {
                Id = newDocumentId,
                UserId = userId,
                DocumentTypeId = documentTypeId,
                Name = documentName,
                FilePath = fullPath, // Salvăm calea către fișierul .enc
                FileHash = hash,     // Hash-ul este cel al PDF-ului original
                FileType = "application/pdf", // Pentru browser, el va rămâne un PDF la descărcare
                UploadedDate = DateTime.UtcNow,
                IsOnBlockChain = false
            };

            await _context.CitizenDocuments.AddAsync(document);
            await _context.SaveChangesAsync();

            return document;
        }

        // ==============================================================================
        // METODE NOI PENTRU COMUNICAREA CU FUNCȚIONARUL
        // ==============================================================================

        public async Task<PagedResult<DocumentRequestResponseDto>> GetPagedMyRequestsAsync(string citizenUserId, int pageNumber, int pageSize)
        {
            var query = _context.DocumentRequests
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
                    // Aici afișăm adresa/numele funcționarului ca să știe cetățeanul cine îi cere actul
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
            var request = await _context.DocumentRequests
                .FirstOrDefaultAsync(r => r.Id == dto.RequestId && r.CitizenId == citizenUserId);

            if (request == null) throw new Exception("Cererea nu a fost găsită sau nu îți aparține.");
            if (request.Status != RequestStatus.Pending) throw new Exception("Această cerere a primit deja un răspuns.");

            request.ResponseDate = DateTime.UtcNow;

            if (dto.IsApproved)
            {
                if (dto.DocumentFile == null || dto.DocumentFile.Length == 0)
                    throw new Exception("Trebuie să încarci un document pentru a aproba cererea.");

                // 1. Păstrăm standardul tău de securitate: Conversie la PDF
                byte[] pdfBytes;
                try
                {
                    pdfBytes = await PdfConversionHelper.ConvertToPdfAsync(dto.DocumentFile);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"Eroare la procesarea fișierului: {ex.Message}");
                }

                // 2. Criptăm documentul exact ca la Portofelul Digital
                var encryptedBytes = EncryptionHelper.Encrypt(pdfBytes);

                // 3. Creăm un folder separat pentru răspunsurile la cereri (opțional, dar mai curat)
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Requests", citizenUserId);
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // 4. Salvăm fișierul fizic .enc
                var uniqueFileName = $"{Guid.NewGuid()}.enc";
                var fullPath = Path.Combine(folderPath, uniqueFileName);

                await File.WriteAllBytesAsync(fullPath, encryptedBytes);

                // 5. Actualizăm cererea
                request.DocumentPath = fullPath;
                request.Status = RequestStatus.Approved;
            }
            else
            {
                // Fluxul de respingere
                if (string.IsNullOrWhiteSpace(dto.RejectionReason))
                    throw new Exception("Trebuie să oferi un motiv pentru refuz.");

                request.RejectionReason = dto.RejectionReason;
                request.Status = RequestStatus.Rejected;
            }

            await _context.SaveChangesAsync();
            return request;
        }
        
    }
}