using MentoraPlatform.Models;
using MentoraPlatform.Services;
using Microsoft.AspNet.Identity;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace MentoraPlatform.Controllers
{
    [Authorize]
    public class GradesController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        [Authorize(Roles = "Student")]
        public ActionResult MyResults(int? courseId, int? quizId)
        {
            var userId = User.Identity.GetUserId();
            var query = db.QuizResults.Include(r => r.Quiz.Course)
                          .Where(r => r.StudentId == userId);

            if (courseId.HasValue) query = query.Where(r => r.Quiz.CourseId == courseId.Value);
            if (quizId.HasValue) query = query.Where(r => r.QuizId == quizId.Value);

            // Populează dropdown-urile doar cu testele susținute de acest student
            ViewBag.Courses = new SelectList(query.Select(r => r.Quiz.Course).Distinct(), "Id", "Title", courseId);

            return View(query.OrderByDescending(r => r.DateTaken).ToList());
        }

        [Authorize(Roles = "Professor, Admin")]
        public ActionResult StudentsGrades(int? courseId, int? quizId, string studentName)
        {
            var userId = User.Identity.GetUserId();
            bool isAdmin = User.IsInRole("Admin");

            // Asigură-te că includem Studentul în query
            var query = db.QuizResults
                          .Include(r => r.Quiz.Course)
                          .Include(r => r.Student) // FOARTE IMPORTANT: Trebuie inclus pentru filtrare
                          .Where(r => isAdmin || r.Quiz.Course.TeacherId == userId);

            if (courseId.HasValue)
                query = query.Where(r => r.Quiz.CourseId == courseId.Value);

            if (quizId.HasValue)
                query = query.Where(r => r.QuizId == quizId.Value);

            // Filtrare după nume - am adăugat ToLower() pentru a fi sigur că se potrivește
            if (!string.IsNullOrEmpty(studentName))
            {
                string name = studentName.ToLower();
                query = query.Where(r => r.Student.FirstName.ToLower().Contains(name) ||
                                         r.Student.LastName.ToLower().Contains(name));
            }

            var results = query.OrderByDescending(r => r.DateTaken).ToList();

            ViewBag.Courses = new SelectList(db.Courses.Where(c => isAdmin || c.TeacherId == userId), "Id", "Title", courseId);
            ViewBag.Quizzes = new SelectList(db.Quizzes.Where(q => isAdmin || q.Course.TeacherId == userId), "Id", "Title", quizId);

            return View(results);
        }

        [Authorize(Roles = "Professor, Admin")]
        public ActionResult StudentRiskDashboard(int courseId)
        {
            var course = db.Courses.Include(c => c.EnrolledStudents).FirstOrDefault(c => c.Id == courseId);
            if (course == null) return HttpNotFound();

            var progressService = new ProgressService();

            // Transformăm studenții în ViewModel-uri de risc folosind noul serviciu
            var list = course.EnrolledStudents.Select(s => {
                var risk = progressService.GetStudentRisk(s.Id, courseId);
                risk.FullName = $"{s.FirstName} {s.LastName}";
                return risk;
            }).ToList();

            ViewBag.CourseTitle = course.Title;
            return View(list);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}