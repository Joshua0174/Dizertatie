using BusinessLayer.DTOs; // <--- OBLIGATORIU: Aici e DTO-ul care rezolvă eroarea Swagger
using BusinessLayer.Helpers;
using BusinessLayer.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Citizen")] // Doar cetățenii au acces aici
    public class DocumentsController : ControllerBase
    {
        private readonly ICitizenDocumentService _documentService;

        public DocumentsController(ICitizenDocumentService docService)
        {
            _documentService = docService;
        }

        [HttpGet("my-documents")]
        public async Task<IActionResult> GetMyDocs()
        {
            var userId = User.FindFirst("Id")?.Value;

            // 1. Luăm datele din bază (care includ User-ul problematic)
            var docs = await _documentService.GetUserDocumentsAsync(userId);

            // 2. LE TRANSFORMĂM ÎN DTO (Aici e reparația pentru Swagger 500!)
            // Nu returnăm Entitatea direct, ci doar datele simple.
            var dtoList = docs.Select(d => new CitizenDocumentDto
            {
                Id = d.Id,
                Name = d.Name,
                FileType = d.FileType,
                UploadedDate = d.UploadedDate,
                FileHash = d.FileHash
            }).ToList();

            return Ok(dtoList);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocument([FromForm] DocumentUploadDto model)
        {
            // 1. Validare rapidă
            if (model.File == null || model.File.Length == 0)
            {
                return BadRequest("Nu ai selectat niciun fișier.");
            }

            // 2. Extragere UserId din Token
            // Asigură-te că în Token-ul tău cheia este într-adevăr "Id" sau ClaimTypes.NameIdentifier
            var userId = User.FindFirst("Id")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Utilizatorul nu a putut fi identificat.");
            }

            try
            {
                // --- FIX-UL AICI: Adăugăm model.DocumentTypeId ca al 4-lea parametru ---
                var result = await _documentService.UploadDocumentAsync(
                    userId,
                    model.File,
                    model.DocumentName,
                    model.DocumentTypeId // <--- Aceasta este cheia succesului acum
                );

                return Ok(new
                {
                    Id = result.Id,
                    Name = result.Name,
                    DocumentTypeId = result.DocumentTypeId,
                    FileType = result.FileType,
                    BlockchainHash = result.FileHash,
                    Status = "Pending Blockchain",
                    Message = "Document salvat și convertit în PDF cu succes!"
                });
            }
            catch (ArgumentException ex)
            {
                // Erori de validare (ex: tip document invalid)
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // Aici intră erorile de bază de date sau de scriere pe disk
                // Loghează excepția ex pentru debugging dacă poți
                return StatusCode(500, $"A apărut o eroare internă: {ex.Message}");
            }
        }

        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadDocument(Guid id)
        {
            var userId = User.FindFirst("Id")?.Value;

            // 1. Căutăm documentul (Verificare drepturi IDOR)
            var document = await _documentService.GetDocumentByIdAsync(id, userId);

            if (document == null)
            {
                return NotFound("Documentul nu a fost găsit sau nu îți aparține.");
            }

            // 2. Verificăm fișierul fizic
            if (!System.IO.File.Exists(document.FilePath))
            {
                return NotFound("Fișierul fizic nu a fost găsit pe server.");
            }

            // 3. Citim fișierul CRIPTAT de pe disc (.enc)
            var encryptedFileBytes = await System.IO.File.ReadAllBytesAsync(document.FilePath);

            // =======================================================
            // 3.1. DECRIPTAREA FIȘIERULUI ÎN MEMORIE
            // =======================================================
            byte[] decryptedPdfBytes;
            try
            {
                // Transformăm fișierul indescifrabil înapoi în format PDF curat
                decryptedPdfBytes = EncryptionHelper.Decrypt(encryptedFileBytes);
            }
            catch (Exception)
            {
                // Dacă fișierul e corupt sau cheia de criptare nu se potrivește
                return StatusCode(500, "Eroare internă: Documentul nu a putut fi decriptat. Posibilă corupere a datelor.");
            }

            // =======================================================
            // --- PASUL DE DISERTAȚIE: Verificarea Integrității ---
            // =======================================================
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                // ATENȚIE: Calculăm hash-ul pe bytes-urile DECRIPTATE (PDF-ul real)
                var currentHashBytes = sha256.ComputeHash(decryptedPdfBytes);
                var currentHash = BitConverter.ToString(currentHashBytes).Replace("-", "").ToLowerInvariant();

                if (currentHash != document.FileHash)
                {
                    // Dacă hash-ul nu coincide, înseamnă că fișierul a fost alterat pe server!
                    return BadRequest("Atenție! Integritatea fișierului a fost compromisă. Documentul a fost modificat neautorizat pe server.");
                }
            }
            // ----------------------------------------------------

            // 4. Pregătim numele pentru download
            var downloadName = document.Name;
            if (!downloadName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                downloadName += ".pdf";
            }

            // 5. Returnăm fișierul DECRIPTAT către utilizator (browserul primește PDF-ul curat)
            return File(decryptedPdfBytes, "application/pdf", downloadName);
        }
    }
}