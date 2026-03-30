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

        
        [Required]
        public bool IsApproved { get; set; }

       
        public string? RejectionReason { get; set; }

        public Guid? SelectedDocumentId { get; set; }
    }
}
