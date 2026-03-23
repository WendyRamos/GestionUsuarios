using GestionUsuarios.Data;
using GestionUsuarios.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using GestionUsuarios.Models;

namespace GestionUsuarios.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        [Authorize]
        [HttpGet]
        public IActionResult KeepAlive()
        {
            // Al llegar aquí, el middleware de ASP.NET actualiza el tiempo de la cookie
            return Json(new { success = true, message = "Sesión renovada" });
        }
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var employee = _context.Employee
                .FirstOrDefault(e => e.UserId == user.Id);

            return View(employee);
        }

        // GET: Editar
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);

            var employee = _context.Employee
                .FirstOrDefault(e => e.UserId == user.Id);

            return View(employee);
        }

        // POST: Guardar cambios
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Employee model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var employee = _context.Employee
                .Include(e => e.User)
                .FirstOrDefault(e => e.UserId == user.Id);

            if (employee == null)
                return NotFound();

            employee.Name = model.Name;
            employee.PaternalSurname = model.PaternalSurname;
            employee.MaternalSurname = model.MaternalSurname;
            employee.Birthdate = model.Birthdate;
            employee.SecondaryEmail = model.SecondaryEmail;
            employee.Phone = model.Phone;
            employee.ScondaryPhone = model.ScondaryPhone;
            employee.TypeContract = model.TypeContract;
            employee.HiringDate = model.HiringDate;

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
