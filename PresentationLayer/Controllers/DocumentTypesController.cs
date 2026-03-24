using BusinessLayer.Interfaces;
using DataAccessLayer.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PresentationLayer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DocumentTypesController : ControllerBase
    {
        private readonly IDocumentTypesService _documentTypesService;
        public DocumentTypesController(IDocumentTypesService documentTypesService)
        {
            _documentTypesService = documentTypesService;
        }

        [HttpGet("for-dropdown")]
        [ResponseCache(Duration = 300)]
        public async Task<IActionResult> GetDocumentTypesForDropdown()
        {
            var documentTypes = await _documentTypesService.GetDocumentTypesForDropdownAsync();            
            return Ok(documentTypes);
        }
    }
}
