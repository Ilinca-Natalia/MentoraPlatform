using MentoraPlatform.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MentoraPlatform.Controllers
{
    // Doar utilizatorii cu rolul de Administrator au dreptul să acceseze panoul de control
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private ApplicationUserManager _userManager;

        public UsersController()
        {
        }

        public UsersController(ApplicationUserManager userManager)
        {
            UserManager = userManager;
        }

        // Proprietatea nativă Owin pentru a gestiona utilizatorii și rolurile acestora în siguranță
        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        // GET: Users
        public ActionResult Index()
        {
            // AUTO-SEEDING: Creăm automat rolurile de bază în SQL Server dacă acestea lipsesc din tabela AspNetRoles
            // Acest pas previne erorile de tip Foreign Key la salvarea modificărilor în baza de date
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(db));

            if (!roleManager.RoleExists("Admin"))
            {
                roleManager.Create(new IdentityRole("Admin"));
            }
            if (!roleManager.RoleExists("Professor"))
            {
                roleManager.Create(new IdentityRole("Professor"));
            }
            if (!roleManager.RoleExists("Student"))
            {
                roleManager.Create(new IdentityRole("Student"));
            }

            // Preluăm toți utilizatorii din baza de date SQL
            var users = db.Users.ToList();
            return View(users);
        }

        // POST: Users/ChangeRole
        [HttpPost]
        [ValidateAntiForgeryToken] // Previne atacurile de tip CSRF/XSRF în timpul operațiunilor de securitate
        public async Task<ActionResult> ChangeRole(string userId, string newRole)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(newRole))
            {
                return Json(new { success = false, message = "Datele trimise spre server sunt incomplete!" });
            }

            // Securitate Critică: Prevenim auto-demiterea (Adminul nu își poate schimba propriul rol activ)
            string currentUserId = User.Identity.GetUserId();
            if (userId == currentUserId)
            {
                return Json(new { success = false, message = "Securitate: Nu vă puteți modifica propriul rol de administrator în timpul unei sesiuni active!" });
            }

            var user = await UserManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "Utilizatorul nu a fost găsit în baza de date locală!" });
            }

            // 1. Preluăm toate rolurile active ale utilizatorului curent
            var currentRoles = await UserManager.GetRolesAsync(userId);

            // 2. Eliminăm utilizatorul din toate rolurile sale curente (pentru a evita asocierile multiple conflictuale)
            if (currentRoles.Any())
            {
                var removeResult = await UserManager.RemoveFromRolesAsync(userId, currentRoles.ToArray());
                if (!removeResult.Succeeded)
                {
                    return Json(new { success = false, message = "Eroare la ștergerea vechilor roluri de securitate!" });
                }
            }

            // 3. Înregistrăm utilizatorul în noul rol selectat de Admin (ex: Student, Professor, Admin)
            var addResult = await UserManager.AddToRoleAsync(userId, newRole);
            if (addResult.Succeeded)
            {
                return Json(new { success = true, message = $"Rolul utilizatorului {user.FirstName} {user.LastName} a fost salvat ca '{newRole}' cu succes în baza de date SQL!" });
            }

            return Json(new { success = false, message = "Eroare la înregistrarea noului rol în tabela AspNetUserRoles!" });
        }

        // GET: Users/Delete/5
        public ActionResult Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var user = db.Users.Find(id);
            if (user == null) return HttpNotFound();

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            // Prevenim eliminarea propriului cont de admin din panou
            if (id == User.Identity.GetUserId())
            {
                TempData["ErrorMessage"] = "Nu vă puteți șterge propriul cont administrativ activ!";
                return RedirectToAction("Index");
            }

            var user = db.Users.Find(id);
            if (user != null)
            {
                db.Users.Remove(user);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Contul utilizatorului a fost șters definitiv din baza de date.";
            }
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}