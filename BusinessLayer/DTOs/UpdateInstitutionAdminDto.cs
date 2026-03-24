using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs
{
    public interface UpdateInstitutionAdminDto
    { 
        public string FullName { get; set; }
        public Guid? NewInstitutionId{ get; set;}
    } 
}
