using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Helpers
{
    public static class HashHelper
    { 
            public static async Task<string> CalculateSha256Async(IFormFile file) {
                
                using var sha256 = SHA256.Create();
                using var stream = file.OpenReadStream();

                var hashBytes = await sha256.ComputeHashAsync(stream);

                
                return Convert.ToHexString(hashBytes).ToLowerInvariant();
             }
    }
}
