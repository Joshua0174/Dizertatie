using BusinessLayer.DTOs; // <--- OBLIGATORIU: Aici e DTO-ul care rezolvă eroarea Swagger
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
        // Aici e schimbarea: primim UN SINGUR parametru [FromForm] care le conține pe ambele
        public async Task<IActionResult> UploadDocument([FromForm] DocumentUploadDto model)
        {
            // Validare automată
            if (model.File == null || model.File.Length == 0)
            {
                return BadRequest("Nu ai selectat niciun fișier.");
            }

            var userId = User.FindFirst("Id")?.Value;

            try
            {
                // Atenție: trimitem model.File și model.DocumentName
                var result = await _documentService.UploadDocumentAsync(userId, model.File, model.DocumentName);

                return Ok(new
                {
                    Id = result.Id,
                    Name = result.Name,
                    FileType = result.FileType,
                    BlockchainHash = result.FileHash,
                    Message = "Document salvat și convertit în PDF cu succes!"
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "A apărut o eroare internă.");
            }
        }

        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadDocument(Guid id)
        {
            var userId = User.FindFirst("Id")?.Value;

            // 1. Căutăm documentul (Verificare drepturi)
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

            // 3. Citim fișierul
            var fileBytes = await System.IO.File.ReadAllBytesAsync(document.FilePath);

            // 4. Asigurăm extensia corectă (.pdf)
            var downloadName = document.Name;
            if (!downloadName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                downloadName += ".pdf";
            }

            // Returnăm fișierul cu Content-Type 'application/pdf' și numele corect
            return File(fileBytes, "application/pdf", downloadName);
        }
    }
}