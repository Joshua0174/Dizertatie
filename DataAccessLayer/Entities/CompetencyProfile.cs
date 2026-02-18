using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    public class CompetencyProfile
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Description { get; set; } 
        public ICollection<OfficialProfile> Officials { get; set; }
        public ICollection<ProfileDocumentType> AllowedDocumentTypes { get; set; }
    }
}
