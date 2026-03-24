using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs
{
    public class OfficialProfileDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string EmployeeCode { get; set; }
        public string CompetencyProfileName { get; set; } // Numele profilului, nu ID-ul!
    }
}
