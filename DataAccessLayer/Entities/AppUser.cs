

using Microsoft.AspNetCore.Identity;

namespace DataAccessLayer.Entities
{
    public class AppUser: IdentityUser
    {
        public string FullName { get; set; }
        public UserRole Role { get; set; }

        public string? BlockChainWalletAddress { get; set; }

        public virtual CitizenProfile? CitizenProfile { get; set; }
        public virtual OfficialProfile? OfficialProfile { get; set; }
    }
}
