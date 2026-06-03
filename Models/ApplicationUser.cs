using Microsoft.AspNetCore.Identity;

namespace MVAManagement.Models
{
    public class ApplicationUser : IdentityUser
    {
        // [AspNetUsers] table
        // IdentityUser already includes:
        // Id, Email, EmailConfirmed, PasswordHash, SecurityStamp,
        // PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled,
        // LockoutEnd, LockoutEnabled, AccessFailedCount, UserName
        public DateTime? LastLoginAt { get; set; }
    }
}