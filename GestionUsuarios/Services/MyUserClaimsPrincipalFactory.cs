using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using GestionUsuarios.Models;

namespace GestionUsuarios.Services
{
    public class MyUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser>
    {
        public MyUserClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, optionsAccessor)
        {
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            // 1. Generamos la identidad base
            var identity = await base.GenerateClaimsAsync(user);

            // 2. Buscamos al usuario en la DB incluyendo la tabla Employee
            // Usamos el UserManager para traer la relación que no viene por defecto
            var userWithEmployee = await UserManager.Users
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            // 3. Extraemos los datos con tus nombres de propiedad reales
            string nombreAMostrar = "";

            if (userWithEmployee?.Employee != null)
            {
                var emp = userWithEmployee.Employee;
                // Formato: Apellido Paterno Apellido Materno, Nombre
                nombreAMostrar = $"{emp.PaternalSurname} {emp.MaternalSurname ?? ""}, {emp.Name}".Trim();
            }
            else
            {
                // Si por algo no hay empleado (ej: admin), usamos el UserName (DNI)
                nombreAMostrar = user.UserName ?? "Usuario";
            }

            // 4. Agregamos el Claim
            identity.AddClaim(new Claim("Nombres", nombreAMostrar));

            return identity;
        }
    }
}