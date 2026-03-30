using BusinessLayer.DTOs;
using BusinessLayer.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PresentationLayer.Hubs;

namespace PresentationLayer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Official")]
    public class OfficialDocumentsController : ControllerBase
    {
        private readonly IOfficialDocumentService _service;
        private readonly IHubContext<NotificationHub> _hubContext;
        public OfficialDocumentsController(IOfficialDocumentService service, IHubContext<NotificationHub> hubContext)
        {
            _service = service;
            _hubContext = hubContext;
        }

        // Metodă helper pentru a extrage curat ID-ul din JWT
        private string GetOfficialId()
        {
            var id = User.FindFirst("Id")?.Value;
            if (string.IsNullOrEmpty(id))
                throw new UnauthorizedAccessException("Token invalid sau utilizator nelogat.");
            return id;
        }

        [HttpPost("send-request")]
        public async Task<IActionResult> SendRequest([FromBody] CreateDocumentRequestDto dto)
        {
            try
            {
                var request = await _service.SendRequestAsync(dto, GetOfficialId());

                if (request.CitizenId != null)
                {
                    string targetGroup = request.CitizenId.ToString().ToLowerInvariant();

                    await _hubContext.Clients.Group(targetGroup).SendAsync(
                        "NewRequestReceived",
                        new
                        {
                            Message = "Ai primit o nouă cerere de document!",
                            DocumentTypeId = request.DocumentTypeId
                        }
                    );
                }

                return Ok(new { Message = "Cerere trimisa cu succes!", RequestId = request.Id });
            }
            catch (KeyNotFoundException ex) { return NotFound(new { Message = ex.Message }); }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new { Message = ex.Message }); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpGet("my-requests")]
        public async Task<IActionResult> GetRequests([FromQuery] int page = 1, [FromQuery] int pageSize = 6, [FromQuery] bool todayOnly = false)
        {
            try
            {
                var result = await _service.GetPagedMyRequestsAsync(GetOfficialId(), page, pageSize, todayOnly); return Ok(result);
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new { Message = ex.Message }); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpGet("search-citizen")]
        public async Task<IActionResult> SearchCitizen([FromQuery] string cnp)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cnp))
                    return BadRequest(new { Message = "CNP-ul este obligatoriu." });

                var citizenInfo = await _service.SearchCitizenByCnpAsync(cnp);
                return Ok(citizenInfo);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { Message = ex.Message }); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpGet("allowed-documents")]
        public async Task<IActionResult> GetAllowedDocuments()
        {
            try
            {
                var documents = await _service.GetAllowedDocumentTypesAsync(GetOfficialId());
                return Ok(documents);
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new { Message = ex.Message }); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpGet("download/{requestId}")]
        public async Task<IActionResult> DownloadDocument(Guid requestId)
        {
            try
            {
                var result = await _service.DownloadRequestedDocumentAsync(requestId, GetOfficialId());
                return File(result.FileBytes, "application/pdf", result.FileName);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { Message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var stats = await _service.GetOfficialStatsAsync(GetOfficialId());
                return Ok(stats);
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new { Message = ex.Message }); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        // ==========================================
        // 2. ENDPOINT NOU PENTRU ISTORIC DOSAR
        // ==========================================
        [HttpGet("citizen-history")]
        public async Task<IActionResult> GetCitizenHistory([FromQuery] string cnp)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cnp))
                    return BadRequest(new { Message = "CNP-ul este obligatoriu." });

                var history = await _service.GetCitizenHistoryAsync(GetOfficialId(), cnp);
                return Ok(history);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { Message = ex.Message }); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("resolve/{requestId}")]
        public async Task<IActionResult> ResolveRequest(Guid requestId)
        {
            try
            {
                var result = await _service.ResolveRequestAsync(requestId, GetOfficialId());

                if (!result)
                {
                    return BadRequest(new { Message = "Cererea nu a putut fi soluționată (posibil a expirat sau nu vă aparține)." });
                }

                return Ok(new { Message = "Verificarea a fost finalizată cu succes. Documentul nu mai este accesibil." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}