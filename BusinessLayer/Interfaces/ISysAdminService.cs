using BusinessLayer.DTOs;
using DataAccessLayer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Interfaces
{
    public interface ISysAdminService
    { 
        Task<bool> CreateInstitutionAdminAsync(CreateInstitutionDto dto);
        Task<DocumentType> CreateDocumentTypeAsync(CreateDocumentTypeDto dto);
        Task<bool> ToggleDocumentTypeStatusAsync(Guid id);

        // Vizualizare toți adminii de instituții

        // Ștergere admin
        Task<bool> DeleteInstitutionAdminAsync(string email);

        // Editare admin (ex: îi schimbi numele sau îl muți la altă instituție)

        Task<List<InstitutionDto>> GetAllInstitutionAsync();

        // Modifică din (string email, string newFullName) în:
        Task<bool> UpdateInstitutionAdminAsync(string email, UpdateInstitutionAdminDto dto);

        Task<bool> DeleteInstitutionAsync(Guid institutionId);
        Task<bool> UpdateInstitutionAsync(Guid id, EditInstitutionDto dto);
        Task<List<DocumentType>> GetAllDocumentTypesAsync();
        Task<bool> AssignNewAdminAsync(CreateNewAdminDto dto);
    }
}
