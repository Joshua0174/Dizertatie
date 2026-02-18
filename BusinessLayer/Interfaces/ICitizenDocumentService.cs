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
    }
}
