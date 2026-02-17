using BusinessLayer.Helpers;
using BusinessLayer.Interfaces;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography; // Necesar pentru SHA256
using System.Text;

namespace BusinessLayer.Services
{
    public class CitizenDocumentService : ICitizenDocumentService
    {
        private readonly AppDbContext _context;

        public CitizenDocumentService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<CitizenDocument>> GetUserDocumentsAsync(string userId)
        {
            return await _context.CitizenDocuments
                .Where(d => d.UserId == userId)
                .ToListAsync();
        }

        public async Task<CitizenDocument> GetDocumentByIdAsync(Guid documentId, string userId)
        {
            var document = await _context.CitizenDocuments
                .FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId);

            // Nu aruncăm excepție aici, returnăm null și lăsăm Controller-ul să decidă (NotFound)
            // Sau poți păstra excepția dacă preferi stilul "fail fast"
            return document;
        }

        public async Task<CitizenDocument> UploadDocumentAsync(string userId, IFormFile file, string documentName)
        {
            // 1. Validări
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId), "UserId invalid");
            if (file == null || file.Length == 0) throw new ArgumentNullException(nameof(file), "Fișier gol");

            // 2. CONVERSIE: Orice intră (JPG, PNG, PDF) iese bytes de PDF
            byte[] pdfBytes;
            try
            {
                // Asigură-te că ai clasa PdfConversionHelper creată anterior!
                pdfBytes = await PdfConversionHelper.ConvertToPdfAsync(file);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Eroare la procesarea fișierului: {ex.Message}");
            }

            // 3. HASH: Calculăm hash-ul pe PDF-ul rezultat (bytes)
            string hash;
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(pdfBytes);
                // Convertim bytes la string hexazecimal
                hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }

            // 4. Pregătire cale stocare
            var newDocumentId = Guid.NewGuid();
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", userId); // Am schimbat "UploadedDocuments" in "Uploads" sa fie consistent

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // 5. EXTENSIE: Forțăm extensia .pdf
            var uniqueFileName = $"{newDocumentId}.pdf";
            var fullPath = Path.Combine(folderPath, uniqueFileName);

            // 6. SALVARE FIZICĂ: Scriem bytes-ii de PDF pe disk
            await File.WriteAllBytesAsync(fullPath, pdfBytes);

            // 7. SALVARE DB
            var document = new CitizenDocument
            {
                Id = newDocumentId,
                UserId = userId,
                Name = documentName,
                FilePath = fullPath,
                FileHash = hash,              // Hash-ul PDF-ului final
                FileType = "application/pdf", // Tipul este garantat PDF
                UploadedDate = DateTime.UtcNow,
                IsOnBlockChain = false
            };

            await _context.CitizenDocuments.AddAsync(document);
            await _context.SaveChangesAsync();

            return document;
        }
    }
}