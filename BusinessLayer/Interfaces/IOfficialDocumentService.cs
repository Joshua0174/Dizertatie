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

        // Înlocuiește Task<List<DocumentRequest>> GetMyRequestAsync... cu:
        Task<CitizenSearchResponseDto> SearchCitizenByCnpAsync(string cnp);
        Task<PagedResult<DocumentRequestResponseDto>> GetPagedMyRequestsAsync(string officialUserId, int pageNumber, int pageSize, bool todayOnly = false);
        Task<DocumentRequest> SendRequestAsync(CreateDocumentRequestDto dto, string officialUserId);
        //Task<List<DocumentRequest>> GetMyRequestAsync(string officialUserId);

        Task<List<DocumentTypeDto>> GetAllowedDocumentTypesAsync(string officialUserId);

        // Adaugă asta lângă celelalte metode
        Task<(byte[] FileBytes, string FileName)> DownloadRequestedDocumentAsync(Guid requestId, string officialUserId);

        Task<OfficialStatsDto> GetOfficialStatsAsync(string officialUserId);
        Task<List<CitizenHistoryDto>> GetCitizenHistoryAsync(string officialUserId, string cnp);
        Task<bool> ResolveRequestAsync(Guid requestId, string officialUserId);
    }
}
