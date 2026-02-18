using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    public class ProfileDocumentType
    {
        public Guid CompetencyProfileId { get; set; }    
        public CompetencyProfile CompetencyProfile { get; set; }
        public Guid DocumentTypeId { get; set; }
        public DocumentType DocumentType { get; set; }
    }
}
