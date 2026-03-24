using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs
{
   public class InstitutionDto
    {
        public string Email { get; set; }
        public string FullName { get; set; }
        public string InstitutionName { get; set; }
        public Guid? InstitutionId { get; set; }  

        public string Cui { get; set; }
        public string Address { get; set; }
    }
}
