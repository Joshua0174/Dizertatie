using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs
{
    public class CitizenDocumentDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string FileType { get; set; }
        public DateTime UploadedDate { get; set; }
        public string FileHash { get; set; }
        public Guid DocumentTypeId { get; set; }
    }
}
