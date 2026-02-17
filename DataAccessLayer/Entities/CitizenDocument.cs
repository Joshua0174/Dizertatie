using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    public class CitizenDocument
    {
        [Required]
        public Guid Id { get; set; }
        [Required] public string Name { get; set; }
        [Required] public string FilePath { get; set; }
        [Required] public string FileHash { get; set; }
        [Required] public string FileType { get; set; }
        [Required] public DateTime UploadedDate { get; set; }
        [Required] public bool IsOnBlockChain { get; set; }
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public AppUser User { get; set; }
    }
}
