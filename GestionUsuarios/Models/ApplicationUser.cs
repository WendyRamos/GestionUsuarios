using Microsoft.AspNetCore.Identity;

namespace GestionUsuarios.Models
{
    public class ApplicationUser : IdentityUser
    {
        public virtual Employee? Employee { get; set; }
    }
}
