using BusinessLayer.DTOs;
using BusinessLayer.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;

namespace PresentationLayer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles ="Admin")]
    public class AdminController:ControllerBase
    {
        private readonly IAdminService _adminService;
        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }
        [HttpPost("document-types")]
        public async Task<IActionResult> CreateType([FromBody] CreateDocumentTypeDto dto)
        {
            var result = await _adminService.CreateDocumentTypeAsync(dto);
            return Ok(result);
        }

        [HttpGet("document-types")]
        public async Task<IActionResult> GetAllTypes()
        {
            var result = await _adminService.GetAllDocumentTypesAsync();
            return Ok(result);
        }

        [HttpPut("document-types/{id}/toggle")]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var succes = await _adminService.ToggleDocumentTypeStatusAsync(id);
            if (!succes) return NotFound("Tipul documentului nu a fost gasit");
            return Ok("Statusul a fost actualizat cu succes.");
        } 


            [HttpGet("officials")]
            public async Task<IActionResult> GetOfficials()
            {
             var officials = await _adminService.GetAllOfficialAsync();

            var result = officials.Select(o => new {
                    o.User.FullName,
                    o.User.Email,
                    o.Institution,
                    DepartmentName = o.CompetencyProfile.Name,
                    o.EmployeeCode
                });
            return Ok(result);
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
    }
}
