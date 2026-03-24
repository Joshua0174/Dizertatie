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
