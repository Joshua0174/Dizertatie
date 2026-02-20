using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs
{
    public class RespondRequestDto
    {
      
            [Required]
            public bool IsApproved { get; set; }
        
    }
}
