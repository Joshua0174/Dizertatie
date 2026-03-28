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
        public async Task<IActionResult> GetRequests([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _service.GetPagedMyRequestsAsync(GetOfficialId(), page, pageSize);
                return Ok(result);
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
    }
}