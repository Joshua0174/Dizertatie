using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs
{
    public class CreateDocumentRequestDto
    {
        [Required]
        [StringLength(13, MinimumLength = 13, ErrorMessage = "CNP-ul trebuie să aibă exact 13 caractere.")]
        public string CitizenCnp { get; set; } // Am schimbat din Email în CNP

        [Required]
        public Guid DocumentTypeId { get; set; }

        public string? Reason { get; set; }
    }
}
