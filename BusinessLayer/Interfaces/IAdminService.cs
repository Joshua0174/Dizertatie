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

        Task<List<CompetencyProfile>> GetCompetencyProfilesAsync();
        Task<AppUser> RegisterOfficialAsync(CreateOfficerDto dto);
        Task<List<OfficialProfile>> GetAllOfficialAsync();
    }
}
