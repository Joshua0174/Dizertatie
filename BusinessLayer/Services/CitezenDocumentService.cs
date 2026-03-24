using BusinessLayer.Helpers;
using BusinessLayer.Interfaces;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
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
                .Include(d => d.DocumentType) // Recomandat: include și tipul pentru a-l afișa în UI
                .Where(d => d.UserId == userId)
                .ToListAsync();
        }

        public async Task<CitizenDocument> GetDocumentByIdAsync(Guid documentId, string userId)
        {
            return await _context.CitizenDocuments
                .Include(d => d.DocumentType)
                .FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId);
        }

        public async Task<CitizenDocument> UploadDocumentAsync(string userId, IFormFile file, string documentName, Guid documentTypeId)
        {
            // 1. Validări de bază
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId), "UserId invalid");
            if (file == null || file.Length == 0) throw new ArgumentNullException(nameof(file), "Fișier gol");

            // Verificăm dacă tipul de document ales chiar există în nomenclator
            var typeExists = await _context.DocumentTypes.AnyAsync(t => t.Id == documentTypeId);
            if (!typeExists) throw new ArgumentException("Tipul de document selectat nu este valid.");

            // 2. CONVERSIE: Procesăm fișierul prin Helper-ul de PDF
            byte[] pdfBytes;
            try
            {
                pdfBytes = await PdfConversionHelper.ConvertToPdfAsync(file);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Eroare la procesarea fișierului: {ex.Message}");
            }

            // 3. HASH: Calculăm amprenta digitală SHA256 (Esențial pentru Blockchain)
            // Calculăm hash-ul pe PDF-ul ORIGINAL, CURAT.
            string hash;
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(pdfBytes);
                hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }

            // 4. Pregătire stocare fizică
            var newDocumentId = Guid.NewGuid();
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", userId);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // =======================================================
            // 5. CRIPTAREA ȘI SALVAREA PE DISK (Securitate maximă)
            // =======================================================

            // Criptăm conținutul fișierului PDF cu AES-256
            var encryptedBytes = EncryptionHelper.Encrypt(pdfBytes);

            // Salvăm cu extensia .enc pentru a indica vizual că fișierul este sigilat criptografic
            var uniqueFileName = $"{newDocumentId}.enc";
            var fullPath = Path.Combine(folderPath, uniqueFileName);

            // Scriem pe disk varianta CRIPTATĂ (care e complet ilizibilă fără cheie)
            await File.WriteAllBytesAsync(fullPath, encryptedBytes);

            // =======================================================

            // 6. SALVARE ÎN BAZA DE DATE
            var document = new CitizenDocument
            {
                Id = newDocumentId,
                UserId = userId,
                DocumentTypeId = documentTypeId,
                Name = documentName,
                FilePath = fullPath, // Salvăm calea către fișierul .enc
                FileHash = hash,     // Hash-ul este cel al PDF-ului original
                FileType = "application/pdf", // Pentru browser, el va rămâne un PDF la descărcare
                UploadedDate = DateTime.UtcNow,
                IsOnBlockChain = false
            };

            await _context.CitizenDocuments.AddAsync(document);
            await _context.SaveChangesAsync();

            return document;
        }
    }
}