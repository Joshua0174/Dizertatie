using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    public class DocumentRequest
    {
        [Key]
        public Guid Id { get; set; }

        // Cine cere? (Funcționarul)
        public string OfficialId { get; set; }
        [ForeignKey("OfficialId")]
        public virtual AppUser Official { get; set; }

        // De la cine cere? (Cetățeanul)
        public string CitizenId { get; set; }
        [ForeignKey("CitizenId")]
        public virtual AppUser Citizen { get; set; }

        // Ce cere? (Tipul documentului)
        public Guid DocumentTypeId { get; set; }
        [ForeignKey("DocumentTypeId")]
        public virtual DocumentType DocumentType { get; set; }

        // Starea cererii
        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        // Data cererii
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        // Data răspunsului (când a dat cetățeanul Approve/Reject)
        public DateTime? ResponseDate { get; set; }

        // Dacă a aprobat, aici va fi calea către fișierul încărcat de cetățean
        public string? DocumentPath { get; set; }

        // Motivul respingerii (opțional)
        public string? RejectionReason { get; set; } 
        public string? RequestReason { get; set; }
    }

    public enum RequestStatus
    {
        Pending,
        Approved,
        Rejected,
        Expired, 
        Resolved
    }
}
