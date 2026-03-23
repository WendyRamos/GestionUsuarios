#nullable disable

using GestionUsuarios.Data;
using GestionUsuarios.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GestionUsuarios.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _context; 

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            ILogger<LoginModel> logger,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            ApplicationDbContext context
        )
        {
            _signInManager = signInManager;
            _logger = logger;
            _userManager = userManager;
            _emailSender = emailSender;
            _context = context; 
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "El usuario es obligatorio")]
            [RegularExpression(@"^[0-9]{8}$", ErrorMessage = "Usuario Incorrecto")]
            [Display(Name = "Usuario")]
            public string User { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Recordarme")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("El modelo no es válido.");
                return Page();
            }

            var user = await _userManager.FindByNameAsync(Input.User);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "El usuario ingresado no está registrado.");
                return Page();
            }

            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                ModelState.AddModelError(string.Empty, "Debes confirmar tu cuenta por correo electrónico antes de poder ingresar.");
                return Page();
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, Input.Password);

            if (!passwordValid)
            {
                await _userManager.AccessFailedAsync(user);

                if (await _userManager.IsLockedOutAsync(user))
                {
                    _logger.LogWarning("La cuenta del usuario ha sido bloqueada.");

                    if (!string.IsNullOrEmpty(user.Email))
                    {
                        try
                        {
                            string subject = "Alerta de Seguridad: Cuenta bloqueada temporalmente";
                            string emailBody = $@"
                            <div style='font-family: sans-serif; padding: 20px;'>
                                <h2 style='color: #d9534f;'>Acceso Bloqueado</h2>
                                <p>Tu cuenta ha sido bloqueada por múltiples intentos fallidos.</p>
                            </div>";

                            await _emailSender.SendEmailAsync(user.Email, subject, emailBody);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error enviando correo: {ex.Message}");
                        }
                    }

                    ViewData["MaxAttempts"] = _userManager.Options.Lockout.MaxFailedAccessAttempts;
                    ViewData["ShowLockoutModal"] = "true";
                }

                ModelState.AddModelError(string.Empty, "Contraseña incorrecta. Inténtalo de nuevo.");
                return Page();
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            var employee = _context.Employee
                .FirstOrDefault(e => e.UserId == user.Id);

            var claims = new List<Claim>();
            if (employee != null)
            {
                var fullName = $"{employee.Name} {employee.PaternalSurname} {employee.MaternalSurname ?? ""}";
                claims.Add(new Claim("FullName", fullName));
                claims.Add(new Claim(ClaimTypes.Name, fullName)); 
            }

            await _signInManager.SignInWithClaimsAsync(user, Input.RememberMe, claims);

            _logger.LogInformation("Usuario con DNI {Dni} ha iniciado sesión.", Input.User);

            return LocalRedirect(returnUrl);
        }
    }
}