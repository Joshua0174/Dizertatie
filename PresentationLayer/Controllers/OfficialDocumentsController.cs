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
        public async Task<IActionResult> GetRequests()
        {
            var officialId = User.FindFirst("Id")?.Value;
            var requests = await _service.GetMyRequestAsync(officialId);

            var result = requests.Select(r => new {
                r.Id,
                Citizen = r.Citizen.Email,
                Document = r.DocumentType.Name,
                Status = r.Status.ToString(),
                Date = r.RequestDate,
                Motiv = r.RejectionReason
            });
            return Ok(result);
        }
    }
}
