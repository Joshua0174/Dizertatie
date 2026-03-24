using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
   public class Institution
    { 
        public Guid Id { get; set; }
        public string Name { get; set; }=string.Empty;
        public string CUI { get; set; }=string.Empty;
        public string Address { get; set; } = string.Empty;

        public virtual ICollection<AppUser> Users { get; set; }=new List<AppUser>();
    }
}
