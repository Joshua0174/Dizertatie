using BusinessLayer.DTOs;
using BusinessLayer.Interfaces;
using DataAccessLayer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Services
{
    public class DocumentTypesService : IDocumentTypesService
    {   
        private readonly AppDbContext _context;
        public DocumentTypesService(AppDbContext context)
        {
            _context = context;
        }
        public async Task<List<DocumentTypeDropdownDto>> GetDocumentTypesForDropdownAsync()
        {
            return await _context.DocumentTypes
                                    .Where(t => t.isActive)
                                    .Select(t => new DocumentTypeDropdownDto
                                    {
                                        Id = t.Id,
                                        Name = t.Name
                                    })
                                    .ToListAsync();
        }
    }
}
