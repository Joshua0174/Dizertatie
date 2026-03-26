using BusinessLayer.DTOs;
using BusinessLayer.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Official")]
    public class OfficialDocumentsController : ControllerBase
    {
        private readonly IOfficialDocumentService _service;
        public OfficialDocumentsController(IOfficialDocumentService service)
        {
            _service = service;
        }

        [HttpPost("send-request")]
        public async Task<IActionResult> SendRequest([FromBody] CreateDocumentRequestDto dto)
        {
            try
            {
                var officialId = User.FindFirst("Id")?.Value;
                var request = await _service.SendRequestAsync(dto, officialId);
                return Ok(new { Message = "Cerere trimisa cu succes!", RequestId = request.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("my-requests")]
        public async Task<IActionResult> GetRequests([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var officialId = User.FindFirst("Id")?.Value;
                if (string.IsNullOrEmpty(officialId)) return Unauthorized("Nu ești logat.");

                var result = await _service.GetPagedMyRequestsAsync(officialId, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("search-citizen")]
        public async Task<IActionResult> SearchCitizen([FromQuery] string cnp)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cnp)) return BadRequest("CNP-ul este obligatoriu.");

                var citizenInfo = await _service.SearchCitizenByCnpAsync(cnp);
                return Ok(citizenInfo);
            }
            catch (Exception ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        // --- ENDPOINT-UL LIPSĂ (Pentru Checkbox-urile din React) ---
        [HttpGet("allowed-documents")]
        public async Task<IActionResult> GetAllowedDocuments()
        {
            try
            {
                var officialId = User.FindFirst("Id")?.Value;
                if (string.IsNullOrEmpty(officialId)) return Unauthorized("Nu ești logat.");

                var documents = await _service.GetAllowedDocumentTypesAsync(officialId);
                return Ok(documents);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("download/{requestId}")]
        public async Task<IActionResult> DownloadDocument(Guid requestId)
        {
            try
            {
                var officialId = User.FindFirst("Id")?.Value;
                if (string.IsNullOrEmpty(officialId)) return Unauthorized("Nu ești logat.");

                var result = await _service.DownloadRequestedDocumentAsync(requestId, officialId);

                // Trimitem fișierul decriptat direct către browser
                return File(result.FileBytes, "application/pdf", result.FileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}