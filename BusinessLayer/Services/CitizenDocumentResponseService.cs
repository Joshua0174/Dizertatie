using BusinessLayer.DTOs;
using BusinessLayer.Interfaces;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Services
{
    public class CitizenDocumentResponseService : ICitizenDocumentResponseService
    {
        private readonly AppDbContext _context;
        public CitizenDocumentResponseService(AppDbContext context)
        {
            _context = context;
            
        }
       
        public async Task RespondToRequestAsync(Guid requestId, RespondRequestDto dto, string citizenId, string rootPath)
        {
            var request = await _context.DocumentRequests.FindAsync(requestId);

            if (request == null) throw new Exception("Cererea nu există.");
            if (request.CitizenId != citizenId) throw new Exception("Această cerere nu vă aparține.");
            if (request.Status != RequestStatus.Pending) throw new Exception("Ați răspuns deja la această cerere.");

            if (dto.IsApproved)
            {
                // --- LOGICA AUTOMATĂ ---
                // 1. Vedem ce tip de document a cerut funcționarul (ex: Copie CI)
                var requiredTypeId = request.DocumentTypeId;

                // 2. Căutăm automat în portofelul cetățeanului cel mai recent document de acel tip
                var walletDoc = await _context.CitizenDocuments
                    .Where(d => d.UserId == citizenId && d.DocumentTypeId == requiredTypeId)
                    .OrderByDescending(d => d.UploadedDate) // Îl luăm pe cel mai nou, dacă are mai multe
                    .FirstOrDefaultAsync();

                // 3. Dacă cetățeanul a zis DA, dar nu are documentul încărcat în portofel:
                if (walletDoc == null)
                {
                    throw new Exception("Nu aveți acest tip de document încărcat în portofelul digital! Vă rugăm să îl încărcați mai întâi la secțiunea 'Documentele Mele'.");
                }

                // 4. Dacă există, dăm acces (copiem calea)
                request.Status = RequestStatus.Approved;
                request.ResponseDate = DateTime.UtcNow; // Pornim cronometrul de 5 minute
                request.DocumentPath = walletDoc.FilePath; // Folosim fișierul existent
            }
            else
            {
                // Dacă a zis NU
                request.Status = RequestStatus.Rejected;
                request.ResponseDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }
    }
}
