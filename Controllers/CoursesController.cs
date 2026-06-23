using MentoraPlatform.Models;
using MentoraPlatform.Services;
using Microsoft.AspNet.Identity;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Mvc;

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

            if (course.TeacherId != User.Identity.GetUserId() && !User.IsInRole("Admin"))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            return View(course);
        }

        // POST: Courses/Delete/5
        [Authorize(Roles = "Professor, Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var course = db.Courses
                           .Include(c => c.Lessons)
                           .Include(c => c.EnrolledStudents)
                           .FirstOrDefault(c => c.Id == id);

            if (course != null)
            {
                // 1. Ștergem cererile de înscriere asociate
                var requests = db.EnrollmentRequests.Where(r => r.CourseId == id);
                db.EnrollmentRequests.RemoveRange(requests);

                // 2. Ștergem lecțiile asociate
                db.Lessons.RemoveRange(course.Lessons);

                // 3. Ștergem cursul
                db.Courses.Remove(course);

                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // MODIFICAT: Trimite cererea de înscriere la curs
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
                // Căutăm dacă există deja o cerere pentru acest elev la acest curs
                var existingRequest = db.EnrollmentRequests
                    .FirstOrDefault(r => r.CourseId == id && r.StudentId == userId);

                if (existingRequest == null)
                {
                    // Creăm o cerere nouă în așteptare
                    var request = new EnrollmentRequest
                    {
                        CourseId = id,
                        StudentId = userId,
                        RequestDate = DateTime.Now,
                        IsPending = true,
                        IsApproved = false
                    };
                    db.EnrollmentRequests.Add(request);
                    db.SaveChanges();
                    TempData["SuccessMessage"] = "Solicitarea dumneavoastră de înscriere a fost expediată! Profesorul titular va analiza cererea în cel mai scurt timp.";
                }
                else if (existingRequest.IsPending)
                {
                    TempData["ErrorMessage"] = "Aveți deja o cerere activă în așteptare pentru acest curs.";
                }
                else if (!existingRequest.IsApproved)
                {
                    // Permitem retrimiterea cererii dacă a fost respinsă anterior
                    existingRequest.IsPending = true;
                    existingRequest.IsApproved = false;
                    existingRequest.RequestDate = DateTime.Now;
                    db.Entry(existingRequest).State = EntityState.Modified;
                    db.SaveChanges();
                    TempData["SuccessMessage"] = "Solicitarea dumneavoastră a fost retrimisă cu succes spre analiză!";
                }
            }

            return RedirectToAction("Details", new { id = id });
        }

        // NOU: Aprobare cerere de înscriere de către Profesor
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Professor, Admin")]
        public ActionResult ApproveRequest(int requestId)
        {
            var request = db.EnrollmentRequests.Include(r => r.Course).Include(r => r.Student).FirstOrDefault(r => r.Id == requestId);
            if (request != null)
            {
                var currentUserId = User.Identity.GetUserId();
                if (request.Course.TeacherId != currentUserId && !User.IsInRole("Admin"))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }

                // Schimbăm starea cererii
                request.IsPending = false;
                request.IsApproved = true;
                request.ApprovalDate = DateTime.Now;

                // Adăugăm elevul în mod oficial la curs
                var course = db.Courses.Include(c => c.EnrolledStudents).FirstOrDefault(c => c.Id == request.CourseId);
                var student = db.Users.Find(request.StudentId);
                if (course != null && student != null)
                {
                    if (!course.EnrolledStudents.Any(s => s.Id == student.Id))
                    {
                        course.EnrolledStudents.Add(student);
                    }
                }

                db.SaveChanges();
                TempData["SuccessMessage"] = $"Cererea de înscriere pentru {student.FirstName} {student.LastName} a fost aprobată cu succes!";
            }
            return RedirectToAction("Index", "Manage");
        }

        // NOU: Respingere cerere de înscriere
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Professor, Admin")]
        public ActionResult RejectRequest(int requestId)
        {
            var request = db.EnrollmentRequests.Include(r => r.Course).Include(r => r.Student).FirstOrDefault(r => r.Id == requestId);
            if (request != null)
            {
                var currentUserId = User.Identity.GetUserId();
                if (request.Course.TeacherId != currentUserId && !User.IsInRole("Admin"))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }

                request.IsPending = false;
                request.IsApproved = false;
                db.SaveChanges();
                TempData["SuccessMessage"] = "Cererea de înscriere a fost respinsă.";
            }
            return RedirectToAction("Index", "Manage");
        }

        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var course = db.Courses
                           .Include(c => c.Lessons)
                           .Include(c => c.EnrolledStudents)
                           .Include(c => c.Teacher)
                           .FirstOrDefault(c => c.Id == id);

            if (course == null) return HttpNotFound();

            var currentUserId = User.Identity.GetUserId();
            var progressService = new ProgressService();
            var enrolledIds = course.EnrolledStudents.Select(s => s.Id).ToList();

            // 1. Construim ViewModel-ul de progres
            var progressModel = new CourseProgressViewModel
            {
                CourseId = course.Id,
                CourseTitle = course.Title,
                Lessons = course.Lessons.Select(l => new LessonStatusViewModel
                {
                    LessonId = l.Id,
                    Title = l.Title,
                    IsCompleted = db.UserLessonProgresses.Any(p => p.LessonId == l.Id && p.UserId == currentUserId)
                }).ToList()
            };

            // Calculăm procentajul
            if (progressModel.Lessons.Count > 0)
            {
                double completed = progressModel.Lessons.Count(l => l.IsCompleted);
                progressModel.ProgressPercentage = (completed / progressModel.Lessons.Count) * 100;
            }

            // 2. Construim ViewModel-ul principal
            var model = new CourseDetailsViewModel
            {
                Course = course,
                Progress = progressModel,
                IsEnrolled = enrolledIds.Contains(currentUserId),
                HasPendingRequest = db.EnrollmentRequests.Any(r => r.CourseId == id && r.StudentId == currentUserId)
            };

            // ViewBag-uri pentru datele de administrare
            ViewBag.EnrollmentRequest = db.EnrollmentRequests.FirstOrDefault(r => r.CourseId == id && r.StudentId == currentUserId);
            var studentRoleId = db.Roles.FirstOrDefault(r => r.Name == "Student")?.Id;

            ViewBag.RawStudentList = db.Users
                        .Where(u => u.Roles.Any(r => r.RoleId == studentRoleId))
                        .Where(u => u.Id != course.TeacherId)
                        .Where(u => !enrolledIds.Contains(u.Id))
                        .ToList();

            return View(model);
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

                // Opțional: Ștergem și istoricul aprobărilor în caz de excludere manuală
                var req = db.EnrollmentRequests.FirstOrDefault(r => r.CourseId == courseId && r.StudentId == studentId);
                if (req != null)
                {
                    db.EnrollmentRequests.Remove(req);
                }

                db.SaveChanges();
                TempData["SuccessMessage"] = "Elev eliminat cu succes din cadrul disciplinei.";
            }
            return RedirectToAction("Details", new { id = courseId });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Professor, Admin")]
        public ActionResult AddStudent(int courseId, string studentEmail)
        {
            var course = db.Courses.Include(c => c.EnrolledStudents).FirstOrDefault(c => c.Id == courseId);
            var student = db.Users.FirstOrDefault(u => u.Email == studentEmail);

            if (course != null && student != null)
            {
                // Verificăm dacă e deja înscris
                if (!course.EnrolledStudents.Any(s => s.Id == student.Id))
                {
                    course.EnrolledStudents.Add(student);

                    // Opțional: Creăm o cerere aprobată pentru evidență
                    var req = new EnrollmentRequest
                    {
                        CourseId = courseId,
                        StudentId = student.Id,
                        IsPending = false,
                        IsApproved = true,
                        RequestDate = DateTime.Now
                    };
                    db.EnrollmentRequests.Add(req);

                    db.SaveChanges();
                    TempData["SuccessMessage"] = "Studentul a fost înscris cu succes!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Studentul este deja înscris la acest curs.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Studentul sau Cursul nu a fost găsit.";
            }

            return RedirectToAction("Details", new { id = courseId });
        }
        [HttpPost]
        public async Task<ActionResult> Chat(string message)
        {
            // Includem tot ce avem nevoie pentru context
            var courses = db.Courses.Include(c => c.Lessons).ToList();

            string context = "Baza de date cursuri: ";
            foreach (var c in courses)
            {
                // Curățăm conținutul lecțiilor de tag-uri HTML (pentru a economisi tokeni)
                string lessonsSummary = "";
                foreach (var l in c.Lessons)
                {
                    string cleanContent = Regex.Replace(l.Content ?? "", "<.*?>", string.Empty);
                    // Luăm doar primii 100 de caractere din conținutul lecției pentru context
                    lessonsSummary += $"[Lecția: {l.Title}, Detalii: {cleanContent.Substring(0, Math.Min(cleanContent.Length, 100))}...] ";
                }

                context += $"ID: {c.Id} | Curs: {c.Title} | Descriere: {c.Description} | {lessonsSummary}; ";
            }

            var aiService = new AIService();
            var responseFromAi = await aiService.GetCourseRecommendationAsync(message, context);

            int courseId = 0;
            int.TryParse(responseFromAi, out courseId);
            var course = db.Courses.Find(courseId);

            return Json(new
            {
                reply = course != null ? "Îți recomand: " + course.Title : "Nu am găsit un curs care să conțină lecții sau descrieri despre asta.",
                url = course != null ? Url.Action("Details", "Courses", new { id = course.Id }) : "#"
            });
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}