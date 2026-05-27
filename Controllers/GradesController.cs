using MentoraPlatform.Models;
using Microsoft.AspNet.Identity;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace MentoraPlatform.Controllers
{
    [Authorize]
    public class GradesController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // 1. Vizualizare Note Student (MyResults)
        [Authorize(Roles = "Student")]
        public ActionResult MyResults()
        {
            var userId = User.Identity.GetUserId();
            var results = db.QuizResults
                            .Include(r => r.Quiz.Course)
                            .Where(r => r.StudentId == userId)
                            .OrderByDescending(r => r.DateTaken)
                            .ToList();
            return View(results);
        }

        // 2. Vizualizare Note Elevi pentru Profesor (StudentsGrades)
        [Authorize(Roles = "Professor, Admin")]
        public ActionResult StudentsGrades()
        {
            var userId = User.Identity.GetUserId();
            bool isAdmin = User.IsInRole("Admin"); // Calculăm rolul în afara interogării LINQ

            var results = db.QuizResults
                            .Include(r => r.Quiz.Course)
                            .Include(r => r.Student)
                            .Where(r => isAdmin || r.Quiz.Course.TeacherId == userId) // Folosim variabila bool
                            .OrderByDescending(r => r.DateTaken)
                            .ToList();

            return View(results);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}