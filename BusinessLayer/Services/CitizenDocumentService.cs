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

        // În CitizenDocumentService.cs
        public async Task<List<CitizenDocumentDto>> GetUserDocumentsAsync(string userId)
        {
            var docs = await _context.CitizenDocuments
                .Include(d => d.DocumentType)
                    .ThenInclude(dt => dt.Category) // <-- Aici e cheia pentru noua tabelă!
                .Where(d => d.UserId == userId)
                .ToListAsync();

            // MAPAREA SE FACE AICI, ÎN BUSINESS LAYER
            var dtoList = docs.Select(d => new CitizenDocumentDto
            {
                Id = d.Id,
                Name = d.Name,
                DocumentTypeId = d.DocumentTypeId,
                FileType = d.FileType,
                UploadedDate = d.UploadedDate,
                FileHash = d.FileHash,
                Category = d.DocumentType?.Category?.Name ?? "Altele", // Maparea sigură
                AllowMultiple = d.DocumentType?.AllowMultiple ?? false
            }).ToList();

            return dtoList;
        }

        public async Task<CitizenDocument> GetDocumentByIdAsync(Guid documentId, string userId)
        {
            return await _context.CitizenDocuments
                .AsNoTracking()
                .Include(d => d.DocumentType)
                .FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId);
        }

        // În BusinessLayer/Services/CitizenDocumentService.cs

        public async Task<CitizenDocument> CreateDocumentAsync(string userId, DocumentUploadDto dto)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId), "UserId invalid");

            // 1. Verificăm setările tipului de document
            var documentTypeSettings = await _context.DocumentTypes
                .Where(t => t.Id == dto.DocumentTypeId)
                .Select(t => new { t.Id, t.AllowMultiple })
                .FirstOrDefaultAsync();

            if (documentTypeSettings == null)
                throw new ArgumentException("Tipul de document selectat nu este valid.");

            // 2. Regula strictă: Dacă NU permite duplicate, verificăm să nu existe deja!
            if (!documentTypeSettings.AllowMultiple)
            {
                var existingDoc = await _context.CitizenDocuments
                    .AnyAsync(d => d.UserId == userId && d.DocumentTypeId == dto.DocumentTypeId);

                if (existingDoc)
                {
                    throw new InvalidOperationException("Ai deja un document de acest tip încărcat. Te rugăm să folosești opțiunea de Editare pentru a-l actualiza.");
                }
            }

            // 3. Procesare Fișier Nou
            byte[] pdfBytes;
            try { pdfBytes = await PdfConversionHelper.ConvertToPdfAsync(dto.File); }
            catch (Exception ex) { throw new ArgumentException($"Eroare la procesarea fișierului: {ex.Message}"); }

            // 4. Generare Hash și Criptare
            string hash;
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                hash = BitConverter.ToString(sha256.ComputeHash(pdfBytes)).Replace("-", "").ToLowerInvariant();
            }
            var encryptedBytes = EncryptionHelper.Encrypt(pdfBytes);

            // 5. Salvare Fizică
            var documentId = Guid.NewGuid();
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", userId);
            Directory.CreateDirectory(folderPath); // Va crea folderul doar dacă nu există
            var fullPath = Path.Combine(folderPath, $"{documentId}.enc");

            await File.WriteAllBytesAsync(fullPath, encryptedBytes);

            // 6. Salvare în Baza de Date
            var document = new CitizenDocument
            {
                Id = documentId,
                UserId = userId,
                DocumentTypeId = dto.DocumentTypeId,
                Name = dto.DocumentName,
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

        public async Task<CitizenDocument> EditDocumentAsync(Guid documentId, string userId, DocumentEditDto dto)
        {
            // 1. Căutăm documentul existent
            var existingDocument = await _context.CitizenDocuments
                .FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId);

            if (existingDocument == null)
                throw new KeyNotFoundException("Documentul specificat nu a fost găsit sau nu îți aparține.");

            bool hasChanges = false;

            // 2. Actualizăm Numele (dacă s-a trimis și e diferit)
            if (!string.IsNullOrWhiteSpace(dto.DocumentName) && existingDocument.Name != dto.DocumentName)
            {
                existingDocument.Name = dto.DocumentName;
                hasChanges = true;
            }

            // 3. Procesăm Noul Fișier (Dacă cetățeanul a încărcat unul nou)
            if (dto.File != null && dto.File.Length > 0)
            {
                byte[] pdfBytes;
                try { pdfBytes = await PdfConversionHelper.ConvertToPdfAsync(dto.File); }
                catch (Exception ex) { throw new ArgumentException($"Eroare la procesarea fișierului: {ex.Message}"); }

                string newHash;
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    newHash = BitConverter.ToString(sha256.ComputeHash(pdfBytes)).Replace("-", "").ToLowerInvariant();
                }

                var encryptedBytes = EncryptionHelper.Encrypt(pdfBytes);

                // Păstrăm același ID și aceeași cale, deci fișierul fizic .enc va fi suprascris curat
                await File.WriteAllBytesAsync(existingDocument.FilePath, encryptedBytes);

                // Actualizăm metadatele în entitate
                existingDocument.FileHash = newHash;
                existingDocument.UploadedDate = DateTime.UtcNow;
                existingDocument.IsOnBlockChain = false; // Fișier modificat -> necesită o nouă validare Blockchain
                hasChanges = true;
            }

            // 4. Salvăm modificările doar dacă s-a schimbat ceva
            if (hasChanges)
            {
                _context.CitizenDocuments.Update(existingDocument);
                await _context.SaveChangesAsync();
            }

            return existingDocument;
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

                    CitizenEmail = r.Citizen.Email,
                    // --- NOU: Extragem numele complet al cetățeanului ---
                    CitizenName = r.Citizen.FullName,

                    OfficialEmail = r.Official.Email,
                    OfficialName = r.Official.FullName,

                    InstitutionName = r.Official.Institution != null ? r.Official.Institution.Name : "Instituție Necunoscută",

                    DocumentName = r.DocumentType.Name,
                    Status = r.Status.ToString(),
                    Date = r.RequestDate,
                    RequestReason = r.RequestReason,
                    RejectionReason = r.RejectionReason,
                    DocumentTypeId = r.DocumentTypeId,
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

        public async Task<DocumentRequestDto> RespondToRequestAsync(RespondToRequestDto dto, string citizenUserId)
        {
            var request = await _context.DocumentRequests
                .Include(r => r.DocumentType)
                .FirstOrDefaultAsync(r => r.Id == dto.RequestId && r.CitizenId == citizenUserId);

            if (request == null) throw new KeyNotFoundException("Cererea nu a fost găsită sau nu îți aparține.");
            if (request.Status != RequestStatus.Pending) throw new InvalidOperationException("Această cerere a primit deja un răspuns.");

            request.ResponseDate = DateTime.UtcNow; // Cronometrul de acces

            if (dto.IsApproved)
            {
                CitizenDocument existingDocument = null;

                if (dto.SelectedDocumentId.HasValue && dto.SelectedDocumentId.Value != Guid.Empty)
                {
                    existingDocument = await _context.CitizenDocuments
                        .FirstOrDefaultAsync(d => d.Id == dto.SelectedDocumentId.Value
                                               && d.UserId == citizenUserId
                                               && d.DocumentTypeId == request.DocumentTypeId);

                    if (existingDocument == null)
                    {
                        throw new InvalidOperationException("Documentul selectat nu este valid sau a fost șters.");
                    }
                }
                else
                {
                    existingDocument = await _context.CitizenDocuments
                        .FirstOrDefaultAsync(d => d.UserId == citizenUserId && d.DocumentTypeId == request.DocumentTypeId);

                    if (existingDocument == null)
                    {
                        throw new InvalidOperationException($"Nu ai un document valid de tipul '{request.DocumentType.Name}' încărcat în cont.");
                    }
                }

                request.DocumentPath = existingDocument.FilePath;
                request.Status = RequestStatus.Approved;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.RejectionReason))
                    throw new ArgumentException("Trebuie să oferi un motiv pentru refuz.");

                request.RejectionReason = dto.RejectionReason;
                request.Status = RequestStatus.Rejected;
            }

            await _context.SaveChangesAsync();

            // ========================================================
            // MAPAREA CĂTRE NOUA FORMĂ:
            // ========================================================
            return new DocumentRequestDto
            {
                Id = request.Id,
                OfficialId = request.OfficialId,
                Status = request.Status.ToString() // Transformăm Enum-ul în String aici
            };
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

        // Adaugă această metodă în BusinessLayer/Services/CitizenDocumentService.cs

        public async Task<bool> DeleteDocumentAsync(Guid documentId, string userId)
        {
            // 1. Găsim documentul și ne asigurăm că aparține utilizatorului curent
            var document = await _context.CitizenDocuments
                .FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId);

            if (document == null)
            {
                throw new KeyNotFoundException("Documentul nu a fost găsit sau nu îți aparține.");
            }

            // Păstrăm calea fișierului pentru a-l șterge de pe disk
            var filePath = document.FilePath;

            // 2. Ștergem înregistrarea din baza de date
            _context.CitizenDocuments.Remove(document);
            await _context.SaveChangesAsync();

            // 3. Ștergem fișierul fizic (.enc) de pe server
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    // O bună practică: dacă baza de date s-a actualizat cu succes, 
                    // dar fișierul fizic e blocat de un alt proces, nu "picăm" tot request-ul.
                    // Aici ideal ar fi să loghezi eroarea într-un sistem de logging (ex: Serilog).
                    Console.WriteLine($"Avertisment: Nu s-a putut șterge fișierul fizic la calea {filePath}. Eroare: {ex.Message}");
                }
            }

            return true;
        }
    }
}