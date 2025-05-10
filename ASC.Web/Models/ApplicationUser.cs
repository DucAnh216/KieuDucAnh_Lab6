using Microsoft.AspNetCore.Identity;

namespace ASC.Web.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
    }
}