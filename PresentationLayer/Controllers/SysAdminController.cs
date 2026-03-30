using BusinessLayer.DTOs;
using BusinessLayer.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SysAdmin")]
    public class SysAdminController : ControllerBase
    {
        private readonly ISysAdminService _sysAdminService;
        public SysAdminController(ISysAdminService sysAdminService)
        {
            _sysAdminService = sysAdminService;
        }

        [HttpGet("document-types")]
        public async Task<IActionResult> GetAllDocumentTypes() //folosit
        {
            try
            {
                var documentTypes = await _sysAdminService.GetAllDocumentTypesAsync();
                return Ok(documentTypes);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("create-institution")] //folosit
        public async Task<IActionResult> CreateInstitutionAdmin([FromBody] CreateInstitutionDto dto)
        {
            try
            {
                var result = await _sysAdminService.CreateInstitutionAdminAsync(dto);
                return Ok(new { Message = "Institution and admin user created successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("create-document-type")] //folosit
        public async Task<IActionResult> CreateType([FromBody] CreateDocumentTypeDto dto)
        {
            var result = await _sysAdminService.CreateDocumentTypeAsync(dto);
            return Ok(result);
        }

        

        [HttpPut("document-types/{id}/toggle")] //folosit
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var success = await _sysAdminService.ToggleDocumentTypeStatusAsync(id);
            if (!success) return NotFound(new { message = "Tipul documentului nu a fost găsit." });
            return Ok(new { message = "Statusul a fost actualizat cu succes." });
        }

        [HttpGet("institutions")] //folosit
        public async Task<IActionResult> GetAllInstitution()
        {
            var admins = await _sysAdminService.GetAllInstitutionAsync();

            return Ok(admins);
        }

        [HttpPost("assign-admin")] //folosit 
        public async Task<IActionResult> AssignAdmin([FromBody] CreateNewAdminDto dto)
        {
            try
            {
                var success = await _sysAdminService.AssignNewAdminAsync(dto);
                return Ok(new { message = "Noul administrator a fost asignat cu succes." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpDelete("institution-admins/{email}")] //folosit
        public async Task<IActionResult> DeleteInstitutionAdmin(string email)
        {
            try
            {
                var success = await _sysAdminService.DeleteInstitutionAdminAsync(email);
                return Ok(new { message = "Administratorul a fost șters cu succes." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("institution-admins/{email}")]
        public async Task<IActionResult> UpdateInstitutionAdmin(string email, [FromBody] UpdateInstitutionAdminDto dto)
        {
            try
            {
                var success = await _sysAdminService.UpdateInstitutionAdminAsync(email, dto);
                return Ok(new { message = "Datele au fost actualizate." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }


        [HttpDelete("delete-institution/{id}")] //folosit
        public async Task<IActionResult> DeleteInstitution(Guid id)
        {
            var succes = await _sysAdminService.DeleteInstitutionAsync(id);
            if (!succes) return NotFound(new { Succes = false, Message = "Institutia nu a fost gasita." });


            return Ok(new {Succes=true, Message="Institutia si conturile asociate au fost sterse"});
                    
        }

        [HttpPut("edit-institution/{id}")] //folosit
        public async Task<IActionResult> EditInstitution(Guid id, [FromBody] EditInstitutionDto dto)
        {
            var success = await _sysAdminService.UpdateInstitutionAsync(id, dto);


            if (!success) return NotFound(new { Success = false, Message = "Institutia nu a fost gasita" });

            return Ok(new { Succes = true, Message="Institutia a fost actualizata cu succes" });
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                // Zero cuplare cu baza de date! Controller-ul doar rutează.
                var categories = await _sysAdminService.GetAllCategoriesAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Eroare la aducerea categoriilor: " + ex.Message });
            }
        }
    }
}
