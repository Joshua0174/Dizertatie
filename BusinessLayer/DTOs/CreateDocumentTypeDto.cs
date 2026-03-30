using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs
{
    public class CreateDocumentTypeDto
    {
        [Required] public string Name { get; set; }
        public string Description { get; set; }
        [Required]
        public Guid CategoryId { get; set; }

        public bool AllowMultiple { get; set; }
        [Required]
        public DateTime EffectiveDate { get; set; }


        public DateTime? ExpirationDate { get; set; }
    }
}
