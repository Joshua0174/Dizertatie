using BusinessLayer.DTOs;
using DataAccessLayer.Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Interfaces
{
    public interface ICitizenDocumentService
    {
        Task<CitizenDocument> UploadDocumentAsync(string UserId, IFormFile file, string documentName, Guid documentTypeId);
        Task<List<CitizenDocument>> GetUserDocumentsAsync(string UserId);

        Task<CitizenDocument> GetDocumentByIdAsync(Guid documentId, string userId);


        Task<PagedResult<DocumentRequestResponseDto>> GetPagedMyRequestsAsync(string citizenUserId, int pageNumber, int pageSize);

        // 2. Procesează răspunsul cetățeanului (Aprobă + Încarcă Fișier SAU Respinge + Motiv)
        Task<DocumentRequest> RespondToRequestAsync(RespondToRequestDto dto, string citizenUserId);
    }
}
