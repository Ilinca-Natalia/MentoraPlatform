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

        // GET: Grades/MyResults
        [Authorize(Roles = "Student")]
        public ActionResult MyResults(int? courseId, int? quizId)
        {
            var userId = User.Identity.GetUserId();

            var query = db.QuizResults.Include(r => r.Quiz.Course)
                                      .Where(r => r.StudentId == userId);

            if (courseId.HasValue) query = query.Where(r => r.Quiz.CourseId == courseId.Value);
            if (quizId.HasValue) query = query.Where(r => r.QuizId == quizId.Value);

            ViewBag.Courses = new SelectList(query.Select(r => r.Quiz.Course).Distinct(), "Id", "Title", courseId);

            return View(query.OrderByDescending(r => r.DateTaken).ToList());
        }

        // GET: Grades/StudentsGrades
        [Authorize(Roles = "Professor, Admin")]
        public ActionResult StudentsGrades(int? courseId, int? quizId, string studentName)
        {
            var userId = User.Identity.GetUserId();
            bool isAdmin = User.IsInRole("Admin");

            var query = db.QuizResults
                          .Include(r => r.Quiz.Course)
                          .Include(r => r.Student)
                          .Where(r => isAdmin || r.Quiz.Course.TeacherId == userId);

            if (courseId.HasValue)
                query = query.Where(r => r.Quiz.CourseId == courseId.Value);

            if (quizId.HasValue)
                query = query.Where(r => r.QuizId == quizId.Value);

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

        // GET: Grades/StudentRiskDashboard
        [Authorize(Roles = "Professor, Admin")]
        public ActionResult StudentRiskDashboard(int courseId)
        {
            var course = db.Courses.Include(c => c.EnrolledStudents).FirstOrDefault(c => c.Id == courseId);
            if (course == null) return HttpNotFound();

            var progressService = new ProgressService();

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