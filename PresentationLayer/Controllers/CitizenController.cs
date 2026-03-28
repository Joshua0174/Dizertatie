using BusinessLayer.DTOs;
using BusinessLayer.Helpers;
using BusinessLayer.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PresentationLayer.Hubs;

namespace PresentationLayer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Citizen")] // Doar cetățenii au acces aici
    public class CitizenController : ControllerBase
    {
        private readonly ICitizenDocumentService _documentService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public CitizenController(ICitizenDocumentService docService, IHubContext<NotificationHub> hubContext)
        {
            _documentService = docService;
            _hubContext = hubContext;   
        }

        // Metodă helper pentru a extrage curat ID-ul din JWT
        private string GetCitizenId()
        {
            var id = User.FindFirst("Id")?.Value;
            if (string.IsNullOrEmpty(id))
                throw new UnauthorizedAccessException("Token invalid sau utilizator nelogat.");
            return id;
        }

        [HttpGet("my-documents")]
        public async Task<IActionResult> GetMyDocs()
        {
            try
            {
                var docs = await _documentService.GetUserDocumentsAsync(GetCitizenId());

                var dtoList = docs.Select(d => new CitizenDocumentDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    DocumentTypeId = d.DocumentTypeId,
                    FileType = d.FileType,
                    UploadedDate = d.UploadedDate,
                    FileHash = d.FileHash
                }).ToList();

                return Ok(dtoList);
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new { Message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocument([FromForm] DocumentUploadDto model)
        {
            if (model.File == null || model.File.Length == 0)
            {
                return BadRequest(new { Message = "Nu ai selectat niciun fișier." });
            }

            try
            {
                var result = await _documentService.UploadDocumentAsync(
                    GetCitizenId(),
                    model.File,
                    model.DocumentName,
                    model.DocumentTypeId
                );

                return Ok(new
                {
                    Id = result.Id,
                    Name = result.Name,
                    DocumentTypeId = result.DocumentTypeId,
                    FileType = result.FileType,
                    BlockchainHash = result.FileHash,
                    Status = "Pending Blockchain",
                    Message = "Document salvat, criptat și convertit în PDF cu succes!"
                });
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new { Message = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { Message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { Message = $"A apărut o eroare internă: {ex.Message}" }); }
        }

        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadDocument(Guid id)
        {
            try
            {
                var document = await _documentService.GetDocumentByIdAsync(id, GetCitizenId());

                if (document == null)
                {
                    return NotFound(new { Message = "Documentul nu a fost găsit sau nu îți aparține." });
                }

                if (!System.IO.File.Exists(document.FilePath))
                {
                    return NotFound(new { Message = "Fișierul fizic nu a fost găsit pe server." });
                }

                var encryptedFileBytes = await System.IO.File.ReadAllBytesAsync(document.FilePath);

                byte[] decryptedPdfBytes;
                try
                {
                    decryptedPdfBytes = EncryptionHelper.Decrypt(encryptedFileBytes);
                }
                catch (Exception)
                {
                    return StatusCode(500, new { Message = "Eroare internă: Documentul nu a putut fi decriptat. Posibilă corupere a datelor." });
                }

                // Verificarea Integrității
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var currentHashBytes = sha256.ComputeHash(decryptedPdfBytes);
                    var currentHash = BitConverter.ToString(currentHashBytes).Replace("-", "").ToLowerInvariant();

                    if (currentHash != document.FileHash)
                    {
                        return BadRequest(new { Message = "Atenție! Integritatea fișierului a fost compromisă. Documentul a fost modificat neautorizat pe server." });
                    }
                }

                var downloadName = document.Name;
                if (!downloadName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    downloadName += ".pdf";
                }

                return File(decryptedPdfBytes, "application/pdf", downloadName);
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new { Message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { Message = $"Eroare internă: {ex.Message}" }); }
        }

        // ==============================================================================
        // ENDPOINT-URI NOI PENTRU CERERILE DE LA FUNCȚIONARI
        // ==============================================================================

        [HttpGet("my-requests")]
        public async Task<IActionResult> GetMyRequests([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _documentService.GetPagedMyRequestsAsync(GetCitizenId(), page, pageSize);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new { Message = ex.Message }); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        // --- SCHIMBAREA MAJORĂ: Folosim [FromBody] pentru că trimitem doar JSON ---
        [HttpPost("respond")]
        public async Task<IActionResult> RespondToRequest([FromBody] RespondToRequestDto dto)
        {
            try
            {
                // 1. Apelăm serviciul (salvăm aprobarea/refuzul în baza de date și pornim cronometrul)
                var result = await _documentService.RespondToRequestAsync(dto, GetCitizenId());

                // =========================================================
                // 2. MAGIA SIGNALR: Notificăm instant funcționarul!
                // =========================================================
                if (!string.IsNullOrEmpty(result.OfficialId))
                {
                    // MĂSURA DE SIGURANȚĂ: Ne asigurăm că ID-ul funcționarului este formatat cu litere mici
                    string targetGroup = result.OfficialId.ToLowerInvariant();

                    await _hubContext.Clients.Group(targetGroup).SendAsync(
                        "RequestUpdated",
                        new
                        {
                            RequestId = result.Id,
                            NewStatus = result.Status.ToString(),
                            Message = dto.IsApproved ? "Cetățeanul a acceptat cererea!" : "Cetățeanul a refuzat cererea."
                        }
                    );
                }

                // 3. Răspundem cu succes cetățeanului
                return Ok(new
                {
                    Message = dto.IsApproved
                        ? "Cererea a fost aprobată. Funcționarul are acum acces temporar la documentul tău."
                        : "Cererea a fost respinsă și funcționarul a fost notificat.",
                    Status = result.Status.ToString()
                });
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new { Message = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { Message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { Message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetMyStats()
        {
            try
            {
                var stats = await _documentService.GetCitizenStatsAsync(GetCitizenId());
                return Ok(stats);
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new { Message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { Message = "Eroare la obținerea statisticilor." }); }
        }
    }
}
