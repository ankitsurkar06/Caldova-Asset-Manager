using Microsoft.AspNetCore.Identity;

namespace CaldovaAssetManager.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }
    }
}