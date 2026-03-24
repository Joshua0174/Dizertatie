using BusinessLayer.DTOs;
using BusinessLayer.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;

namespace PresentationLayer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles ="InstitutionAdmin")]
    public class AdminController:ControllerBase
    {
        private readonly IAdminService _adminService;
        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("officials")]
        public async Task<IActionResult> GetOfficials()
        {
            var officials = await _adminService.GetAllOfficialAsync();
            return Ok(officials);
        }

       
        [HttpPost("competency-profiles")]
        public async Task<IActionResult> CreateCompetencyProfile([FromBody] CreateCompetencyProfileDto dto)
        {
            try
            {
                var profile = await _adminService.CreateCompetencyProfileAsync(dto);
                return Ok(new { Message = "Profilul a fost creat cu succes.", ProfileId = profile.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "Eroare la crearea profilului.", Error = ex.Message });
            }
        }

        [HttpGet("competency-profiles")]
        public async Task<IActionResult> GetCompetencyProfiles()
        {
            try
            {
                var profiles = await _adminService.GetCompetencyProfilesAsync();
                return Ok(profiles);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "Eroare la aducerea profilurilor.", Error = ex.Message });
            }
        }
        [HttpPost("register-official")]
        public async Task<IActionResult> RegisterOfficial([FromBody] CreateOfficerDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var user = await _adminService.RegisterOfficialAsync(dto);
                return Ok(new
                {
                    Message = "Oficialul a fost înregistrat cu succes.",
                    UserEmail = user.Email,
                    UserId = user.Id
                }
                    );

            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = "Eroare la înregistrarea oficialului. Verificați datele și încercați din nou.",
                    Error = ex.Message
                });
            }
        }

        [HttpGet("system-document-types")]
        public async Task<IActionResult> GetSystemDocumentTypes([FromQuery] int page = 1, [FromQuery] int pageSize = 5,[FromQuery] string search = "")
        {
            try
            {
                var result = await _adminService.GetPagedSystemDocumentTypesAsync(page, pageSize, search);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "Eroare la aducerea documentelor.", Error = ex.Message });
            }
        }
    }
}
