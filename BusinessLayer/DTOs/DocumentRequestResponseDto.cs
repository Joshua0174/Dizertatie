using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs
{
    public class DocumentRequestResponseDto
    {
        public Guid Id { get; set; }
        public string CitizenName { get; set; }
        public string CitizenEmail { get; set; } // Vizibil pentru funcționar
        public string OfficialName { get; set; }
        public string OfficialEmail { get; set; } // Vizibil pentru cetățean
        public string InstitutionName { get; set; } // Vizibil pentru cetățean
        public string DocumentName { get; set; }
        public string Status { get; set; }
        public DateTime Date { get; set; }
        public string RequestReason { get; set; }
        public string RejectionReason { get; set; }
        public DateTime? ResponseDate { get; set; }
        public Guid DocumentTypeId { get; set; }
    }
}
