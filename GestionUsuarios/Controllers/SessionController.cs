using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using GestionUsuarios.Models;

namespace GestionUsuarios.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SessionController : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;

        public SessionController(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
        }

        [HttpGet("refresh")]
        public async Task<IActionResult> Refresh()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var user = await _signInManager.UserManager.GetUserAsync(User);
                if (user != null)
                {
                    await _signInManager.RefreshSignInAsync(user);
                    return Ok(new { message = "Sesión extendida" });
                }
            }
            return Unauthorized();
        }
    }
}