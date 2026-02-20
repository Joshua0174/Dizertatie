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
        [EmailAddress]
        public string CitizenEmail { get; set; }    
        [Required]
        public Guid DocumentTypeId { get; set; }

        public string? Reason { get; set; }
    }
}
