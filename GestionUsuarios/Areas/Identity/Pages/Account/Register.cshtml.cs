// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using GestionUsuarios.Data;
using GestionUsuarios.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;

namespace GestionUsuarios.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _context;
        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _context = context;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            [Required(ErrorMessage = "Este campo es obligatorio")]
            [StringLength(9, MinimumLength = 8, ErrorMessage = "El documento debe tener entre 8 y 9 caracteres")]
            [Display(Name = "DNI")]
            public string User { get; set; }

            [Required(ErrorMessage = "El correo es obligatorio")]
            [EmailAddress(ErrorMessage = "El formato del correo no es válido")]
            [Display(Name = "Email")]
            public string Email { get; set; }
            [Required(ErrorMessage = "Los nombres y apellidos son obligatorios")]
            [Display(Name = "Nombres Completos")]
            public string FullName { get; set; }

            [Required(ErrorMessage = "Debe seleccionar su sexo")]
            public string Sex { get; set; }

            public string Nationality { get; set; }

            [Required(ErrorMessage = "La contraseña es obligatoria")]
            [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} y máximo {1} caracteres.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{6,}$",
    ErrorMessage = "La contraseña debe tener: Mayúscula, Minúscula, Número y un símbolo (!@#$...).")]
            [Display(Name = "Contraseña")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
            public string ConfirmPassword { get; set; }
        }


        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            if (ModelState.IsValid)
            {
                var userCheck = await _userManager.FindByNameAsync(Input.User);
                if (userCheck != null)
                {
                    ModelState.AddModelError(string.Empty, "Este usuario ya se encuentra registrado.");
                    return Page();
                }

                var user = CreateUser();
                await _userStore.SetUserNameAsync(user, Input.User, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    var nameParts = Input.FullName.Trim().Split(' ');
                    string firstName = "", paternal = "", maternal = "";

                    if (nameParts.Length >= 3)
                    {
                        maternal = nameParts[nameParts.Length - 1];
                        paternal = nameParts[nameParts.Length - 2];
                        firstName = string.Join(" ", nameParts.Take(nameParts.Length - 2));
                    }
                    else if (nameParts.Length == 2)
                    {
                        firstName = nameParts[0];
                        paternal = nameParts[1];
                    }
                    else
                    {
                        firstName = nameParts[0];
                    }

                    var newEmployee = new Employee
                    {
                        UserId = user.Id,
                        Name = firstName,
                        PaternalSurname = paternal,
                        MaternalSurname = maternal,
                        TypeDocument = Request.Form["docTypeSelect"],
                        Document = Input.User,
                        Sex = Input.Sex,
                        Nationality = Input.Nationality,
                    };

                    _context.Employee.Add(newEmployee);
                    await _context.SaveChangesAsync();

                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code },
                        protocol: Request.Scheme);
                    string emailBody = $@"
<div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;"">
    <div style=""text-align: center; padding: 20px;"">
        <div style=""background-color: #fff1f1; width: 80px; height: 80px; border-radius: 50%; display: inline-block; line-height: 80px; color: #E89F9F; font-size: 40px;"">
            🚀
        </div>
    </div>
    
    <h2 style=""color: #1a1f36; text-align: center;"">¡Hola, {firstName}!</h2>
    
    <p style=""color: #4f566b; font-size: 16px; line-height: 1.5; text-align: center;"">
        Estamos muy emocionados de tenerte en <strong>GestionUsuarios</strong>. <br>
        Para empezar a explorar la plataforma, solo necesitas confirmar tu cuenta.
    </p>
    
    <div style=""text-align: center; margin: 35px 0;"">
        <a href=""{callbackUrl}"" style=""background-color: #0056b3; color: white; padding: 15px 30px; text-decoration: none; border-radius: 8px; font-weight: bold; display: inline-block;"">
            Activar mi cuenta
        </a>
    </div>
    
    <p style=""color: #a3acb9; font-size: 13px; text-align: center;"">
        Si no solicitaste esta cuenta, puedes ignorar este correo de forma segura.
    </p>
    
    <hr style=""border: 0; border-top: 1px solid #f0f0f0; margin: 20px 0;"">
    
    <p style=""color: #a3acb9; font-size: 12px; text-align: center;"">
        © 2026 GestionUsuarios. Todos los derechos reservados.
    </p>
</div>";

                    await _emailSender.SendEmailAsync(Input.Email, "Confirma tu cuenta", emailBody);

                    return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return Page();
        }
        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}
