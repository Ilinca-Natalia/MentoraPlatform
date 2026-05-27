using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using MentoraPlatform.Models;
using Microsoft.AspNet.Identity;

namespace MentoraPlatform.Controllers
{
    [Authorize] // Nimeni nu vede nimic fără login
    public class CoursesController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Courses
        [AllowAnonymous] // Permitem vizitatorilor să vadă lista de cursuri (promovare)
        public ActionResult Index()
        {
            // Includem Teacher pentru a afișa numele profesorului în listă
            var courses = db.Courses.Include(c => c.Teacher).ToList();
            return View(courses);
        }

        // GET: Courses/Create
        [Authorize(Roles = "Professor, Admin")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Courses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Professor, Admin")]
        public ActionResult Create([Bind(Include = "Title,Description")] Course course)
        {
            if (ModelState.IsValid)
            {
                // ATRIBUIM AUTOMAT PROFESORUL CURENT
                course.TeacherId = User.Identity.GetUserId();
                course.CreatedAt = DateTime.Now;

                db.Courses.Add(course);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(course);
        }

        // GET: Courses/Edit/5
        [Authorize(Roles = "Professor, Admin")]
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Course course = db.Courses.Find(id);
            if (course == null) return HttpNotFound();

            // VERIFICARE SECURITATE: Ești proprietarul sau Admin?
            if (course.TeacherId != User.Identity.GetUserId() && !User.IsInRole("Admin"))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "Nu poți edita cursul altui profesor!");
            }

            return View(course);
        }

        // POST: Courses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Professor, Admin")]
        public ActionResult Edit([Bind(Include = "Id,Title,Description,TeacherId,CreatedAt")] Course course)
        {
            // Re-verificăm securitatea și la POST (pentru a evita hack-urile manuale de ID)
            if (course.TeacherId != User.Identity.GetUserId() && !User.IsInRole("Admin"))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            if (ModelState.IsValid)
            {
                db.Entry(course).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(course);
        }

        // GET: Courses/Delete/5
        [Authorize(Roles = "Professor, Admin")]
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Course course = db.Courses.Find(id);
            if (course == null) return HttpNotFound();

            // SECURITATE
            if (course.TeacherId != User.Identity.GetUserId() && !User.IsInRole("Admin"))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            return View(course);
        }

        // POST: Courses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Professor, Admin")]
        public ActionResult DeleteConfirmed(int id)
        {
            Course course = db.Courses.Find(id);

            // SECURITATE FINALĂ
            if (course.TeacherId != User.Identity.GetUserId() && !User.IsInRole("Admin"))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            db.Courses.Remove(course);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public ActionResult Enroll(int id)
        {
            var course = db.Courses.Find(id);
            var userId = User.Identity.GetUserId();
            var student = db.Users.Find(userId);

            if (course != null && student != null)
            {
                if (!course.EnrolledStudents.Contains(student))
                {
                    course.EnrolledStudents.Add(student);
                    db.SaveChanges();
                }
            }

            return RedirectToAction("Details", new { id = id });
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
       
        [HttpPost]
        [Authorize(Roles = "Professor, Admin")]
        [ValidateAntiForgeryToken]
        public ActionResult AddStudent(int courseId, string studentEmail)
        {
            if (string.IsNullOrEmpty(studentEmail))
            {
                TempData["ErrorMessage"] = "Vă rugăm să introduceți un email.";
                return RedirectToAction("Details", new { id = courseId });
            }

            var student = db.Users.FirstOrDefault(u => u.Email == studentEmail);

            if (student != null)
            {
                var course = db.Courses.Include(c => c.EnrolledStudents).FirstOrDefault(c => c.Id == courseId);
                if (!course.EnrolledStudents.Any(s => s.Id == student.Id))
                {
                    course.EnrolledStudents.Add(student);
                    db.SaveChanges();
                    TempData["SuccessMessage"] = $"Elevul {student.FirstName} a fost înscris!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Elevul este deja înscris la acest curs.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Nu am găsit niciun cont cu acest email.";
            }

            return RedirectToAction("Details", new { id = courseId });
        }
        // GET: Courses/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var course = db.Courses
                           .Include(c => c.Lessons)
                           .Include(c => c.EnrolledStudents)
                           .Include(c => c.Teacher)
                           .FirstOrDefault(c => c.Id == id);

            if (course == null) return HttpNotFound();

            // ID-urile elevilor deja înscriși
            var enrolledIds = course.EnrolledStudents.Select(s => s.Id).ToList();

            // Găsim ID-ul rolului de "Student"
            var studentRoleId = db.Roles.FirstOrDefault(r => r.Name == "Student")?.Id;

            // Filtrare: Să aibă rolul Student, să nu fie profesorul cursului și să nu fie deja înscris
            ViewBag.RawStudentList = db.Users
                .Where(u => u.Roles.Any(r => r.RoleId == studentRoleId)) // Doar Studenți
                .Where(u => u.Id != course.TeacherId)                   // Fără profesorul cursului
                .Where(u => !enrolledIds.Contains(u.Id))                // Fără cei deja înscriși
                .ToList();

            return View(course);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteStudent(int courseId, string studentId)
        {
            var course = db.Courses.Include(c => c.EnrolledStudents).FirstOrDefault(c => c.Id == courseId);
            var student = db.Users.Find(studentId);

            if (course != null && student != null)
            {
                course.EnrolledStudents.Remove(student);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Elev eliminat.";
            }
            return RedirectToAction("Details", new { id = courseId });
        }
    }
}