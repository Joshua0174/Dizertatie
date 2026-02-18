using DataAccessLayer.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PresentationLayer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentTypesController : ControllerBase
    {
        private readonly AppDbContext _context;
        public DocumentTypesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("for-dropdown")]
        [ResponseCache(Duration = 300)]
        public async Task<IActionResult> GetDocumentTypesForDropdown()
        {
            var documentTypes = await _context.DocumentTypes
                .Where(t => t.isActive)
                .Select(t => new { t.Id, t.Name })
                .ToListAsync();
            
            return Ok(documentTypes);
        }
    }
}
