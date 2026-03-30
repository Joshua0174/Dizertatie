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
    public class DocumentType
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public string Name{get; set;}
        public string Description { get; set; }


        [Required]
        public Guid CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual DocumentCategory Category { get; set; }
        public bool AllowMultiple { get; set; } = false; // Implicit fals (Upsert)

        public DateTime EffectiveDate { get; set; }
        public DateTime ?ExpirationDate{ get; set; }
        public bool isActive { get; set; } = true;

    }
}
