using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs
{
    public class EditInstitutionDto
    { 
        public string InstitutionName { get; set; } 
        public string Cui { get; set; }
        public string Address { get; set; }
    }
}
