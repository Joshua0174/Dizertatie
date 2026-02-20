using BusinessLayer.DTOs;
using DataAccessLayer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Interfaces
{
    public interface ICitizenDocumentResponseService
    {
        Task RespondToRequestAsync(Guid requestId, RespondRequestDto dto, string citizenId, string rootPath);
    }
}
