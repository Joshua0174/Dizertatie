using BusinessLayer.DTOs;
using DataAccessLayer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Interfaces
{
    public interface IOfficialDocumentService
    {
        Task<DocumentRequest> SendRequestAsync(CreateDocumentRequestDto dto, string officialUserId);
        Task<List<DocumentRequest>> GetMyRequestAsync(string officialUserId);
    }
}
