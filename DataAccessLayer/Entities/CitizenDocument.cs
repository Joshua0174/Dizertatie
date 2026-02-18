using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    public class CitizenDocument
    {
        
            [Key]
            public Guid Id { get; set; }

            public string UserId { get; set; }
            [ForeignKey("UserId")]
            public AppUser User { get; set; }

            // RELAȚIA CU NOMENCLATORUL
            [Required]
            public Guid DocumentTypeId { get; set; }

            [ForeignKey("DocumentTypeId")]
            public DocumentType DocumentType { get; set; }

            // METADATE FIȘIER
            [Required]
            [MaxLength(200)]
            public string Name { get; set; }

            [Required]
            public string FilePath { get; set; }

            [Required]
            public string FileType { get; set; } // .pdf

            [Required]
            public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

            public DateTime? ExpirationDate { get; set; } // Când expiră actul LUI ION

            // --- BLOCKCHAIN ---
            [Required]
            public string FileHash { get; set; } // SHA256

            [Required]
            public bool IsOnBlockChain { get; set; } = false;

            public string? BlockChainTransactionId { get; set; }
            public long? BlockChainBlockNumber { get; set; }
        

    }  
}
