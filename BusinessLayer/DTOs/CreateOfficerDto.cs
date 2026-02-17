using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;

namespace BusinessLayer.DTOs
{
    public class CreateOfficerDto
    {
        [Required]

        public string Email { get; set; }

        [ Required]
        public string FullName { get; set; }
        [Required]
        [MinLength(6)]
        public string Password { get; set; }

        [Required]
        public string Institution { get; set; }

        [Required]
        public string Department { get; set; }
    }
    
}
