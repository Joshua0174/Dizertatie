using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs
{
    public class DocumentUploadDto
    {
        [Required] public IFormFile File { get; set; }
        [Required] public string DocumentName{ get; set; }
        
    }
}
