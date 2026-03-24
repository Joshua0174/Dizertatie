using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLayer.DTOs
{
    public class AuthResult
    { 
         public bool Success { get; set; }
         public string Token { get; set; }
         public string RefreshToken { get; set; }
         public List<string> Errors { get; set; }  

         public string Role { get; set; }
        public string FullName { get; set; }

    }
}
