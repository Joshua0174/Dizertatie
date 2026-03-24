using BusinessLayer.DTOs;
using DataAccessLayer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Interfaces
{
    public interface IAdminService
    {

        Task<CompetencyProfile> CreateCompetencyProfileAsync(CreateCompetencyProfileDto dto);
        Task<List<CompetencyProfile>> GetCompetencyProfilesAsync();
        Task<AppUser> RegisterOfficialAsync(CreateOfficerDto dto);
        Task<List<OfficialProfileDto>> GetAllOfficialAsync();
        Task<PagedResult<DocumentTypeDto>> GetPagedSystemDocumentTypesAsync(int pageNumber, int pageSize, string searchTerm);
    }
}
