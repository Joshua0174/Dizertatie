using BusinessLayer.DTOs;
using BusinessLayer.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles ="Citizen")]
    public class CitizenDocumentResponesController : ControllerBase
    {
         private readonly ICitizenDocumentResponseService _service;
            public CitizenDocumentResponesController(ICitizenDocumentResponseService service)
            {
                _service = service;
            }

        [HttpPut("respond/{requestId}")]
        public async Task<IActionResult> RespondToRequest(Guid requestId, [FromBody] RespondRequestDto dto)
        {
            try
            {
                var citizenId = User.FindFirst("Id")?.Value;
                await _service.RespondToRequestAsync(requestId, dto, citizenId, "");
                if(dto.IsApproved)
                {
                    return Ok(new { Message = "Cererea a fost aprobată cu succes! Functionarul are 5 minute sa vizualizeze documentul." });
                }
                else return Ok(new { Message = "Cererea a fost respinsa." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
