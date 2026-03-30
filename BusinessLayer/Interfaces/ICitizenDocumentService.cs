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

        Task<List<CitizenDocumentDto>> GetUserDocumentsAsync(string UserId);

        Task<CitizenDocument> GetDocumentByIdAsync(Guid documentId, string userId);


        Task<PagedResult<DocumentRequestResponseDto>> GetPagedMyRequestsAsync(string citizenUserId, int pageNumber, int pageSize);

        // 2. Procesează răspunsul cetățeanului (Aprobă + Încarcă Fișier SAU Respinge + Motiv)
        Task<DocumentRequestDto> RespondToRequestAsync(RespondToRequestDto dto, string citizenUserId);
        Task<CitizenStatsDto> GetCitizenStatsAsync(string citizenId);

      
        Task<CitizenDocument> CreateDocumentAsync(string userId, DocumentUploadDto dto);
        Task<CitizenDocument> EditDocumentAsync(Guid documentId, string userId, DocumentEditDto dto);
        Task<bool> DeleteDocumentAsync(Guid documentId, string userId);
    }
}
