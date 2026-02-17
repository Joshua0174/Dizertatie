using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
namespace BusinessLayer.DTOs
{
    public class RegisterCitizenDto
    {

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }

        [Required] 
        public string FullName{ get; set; }

        [Required]
        public string? CNP { get; set; }
       

    }
}
