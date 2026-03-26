using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs
{
    public class RespondToRequestDto
    {
        [Required]
        public Guid RequestId { get; set; }

        // A zis DA sau a zis NU?
        [Required]
        public bool IsApproved { get; set; }

        // Fișierul efectiv (PDF/JPG). Folosim IFormFile pentru a-l putea salva pe disc.
        // Este opțional (?) pentru că, dacă dă Reject, nu va încărca niciun fișier.
        public IFormFile? DocumentFile { get; set; }

        // Motivul refuzului. Completat doar dacă IsApproved este false.
        public string? RejectionReason { get; set; }
    }
}
