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
    [Authorize(Roles = "Admin")] 
    public class UsersController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private ApplicationUserManager _userManager;

        public UsersController() { }

        public UsersController(ApplicationUserManager userManager)
        {
            UserManager = userManager;
        }

        public ApplicationUserManager UserManager
        {
            get => _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            private set => _userManager = value;
        }

        // GET: Users 
        public ActionResult Index(string searchString, string roleFilter)
        {
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(db));
            if (!roleManager.RoleExists("Admin")) roleManager.Create(new IdentityRole("Admin"));
            if (!roleManager.RoleExists("Professor")) roleManager.Create(new IdentityRole("Professor"));
            if (!roleManager.RoleExists("Student")) roleManager.Create(new IdentityRole("Student"));

            var usersQuery = db.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                string search = searchString.Trim();
                usersQuery = usersQuery.Where(u => u.FirstName.Contains(search) || u.LastName.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(roleFilter))
            {
                usersQuery = usersQuery.Where(u => u.Roles.Any(r => r.RoleId == db.Roles.FirstOrDefault(rol => rol.Name == roleFilter).Id));
            }

            ViewBag.RoleFilter = new SelectList(new[] { "Admin", "Professor", "Student" }, roleFilter);
            ViewBag.SearchString = searchString;

            var users = usersQuery.ToList();
            return View(users);
        }

        // POST: Users/ChangeRole 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangeRole(string userId, string newRole)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(newRole))
            {
                return Json(new { success = false, message = "Datele trimise spre server sunt incomplete!" });
            }

            string currentUserId = User.Identity.GetUserId();
            if (userId == currentUserId)
            {
                return Json(new { success = false, message = "Securitate: Nu vă puteți modifica propriul rol!" });
            }

            var user = await UserManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "Utilizatorul nu a fost găsit!" });
            }

            var currentRoles = await UserManager.GetRolesAsync(userId);
            if (currentRoles.Any())
            {
                var removeResult = await UserManager.RemoveFromRolesAsync(userId, currentRoles.ToArray());
                if (!removeResult.Succeeded)
                {
                    return Json(new { success = false, message = "Eroare la eliminarea vechilor roluri!" });
                }
            }

            var addResult = await UserManager.AddToRoleAsync(userId, newRole);
            if (addResult.Succeeded)
            {
                return Json(new { success = true, message = $"Rolul utilizatorului {user.FirstName} {user.LastName} a fost actualizat la '{newRole}'." });
            }

            return Json(new { success = false, message = "Eroare la actualizarea rolului în baza de date!" });
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
        public async Task<ActionResult> DeleteConfirmed(string id)
        {
            if (id == User.Identity.GetUserId())
            {
                TempData["ErrorMessage"] = "Nu vă puteți șterge propriul cont!";
                return RedirectToAction("Index");
            }

            var user = await UserManager.FindByIdAsync(id);
            if (user != null)
            {
                var projects = db.CodeProjects.Where(p => p.UserId == id);
                db.CodeProjects.RemoveRange(projects);

                var enrollments = db.EnrollmentRequests.Where(r => r.StudentId == id);
                db.EnrollmentRequests.RemoveRange(enrollments);

                var progress = db.UserLessonProgresses.Where(p => p.UserId == id);
                db.UserLessonProgresses.RemoveRange(progress);

                await db.SaveChangesAsync();

                var result = await UserManager.DeleteAsync(user);

                if (result.Succeeded) TempData["SuccessMessage"] = "Utilizator șters cu succes.";
                else TempData["ErrorMessage"] = "Eroare: " + string.Join(", ", result.Errors);
            }
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _userManager?.Dispose();
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}