using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DataAccessLayer.Entities
{
    public class OfficialProfile
    {

        [Key]
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public string Institution { get; set; }
        public string Department { get; set; }

        [ForeignKey("UserId")]
        public virtual AppUser User { get; set; }
    }
}
