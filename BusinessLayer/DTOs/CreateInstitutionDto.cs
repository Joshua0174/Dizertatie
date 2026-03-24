using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs
{
    public class CreateInstitutionDto
    { 
        public string InstitutionName { get; set; }
        public string CUI { get; set; }
        public string Address { get; set; }

        public string AdminEmail { get; set; }
        public string AdminFullName { get; set; }
        public string AdminPassword { get; set; }
    }
}
