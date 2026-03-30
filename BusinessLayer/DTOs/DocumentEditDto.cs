using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs
{
    public class DocumentEditDto
    {
        public string? DocumentName { get; set; }
        public IFormFile? File { get; set; }
    }
}
