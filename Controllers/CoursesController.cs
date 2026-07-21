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
    [Authorize]
    public class CoursesController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Courses 
        [AllowAnonymous]
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
                var requests = db.EnrollmentRequests.Where(r => r.CourseId == id);
                db.EnrollmentRequests.RemoveRange(requests);
                db.Lessons.RemoveRange(course.Lessons);
                db.Courses.Remove(course);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // POST: Courses/Enroll/5 
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
                var existingRequest = db.EnrollmentRequests
                    .FirstOrDefault(r => r.CourseId == id && r.StudentId == userId);

                if (existingRequest == null)
                {
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
                    TempData["SuccessMessage"] = "Solicitarea dumneavoastră a fost expediată!";
                }
                else if (existingRequest.IsPending)
                {
                    TempData["ErrorMessage"] = "Aveți deja o cerere activă în așteptare.";
                }
                else if (!existingRequest.IsApproved)
                {
                    existingRequest.IsPending = true;
                    existingRequest.IsApproved = false;
                    existingRequest.RequestDate = DateTime.Now;
                    db.Entry(existingRequest).State = EntityState.Modified;
                    db.SaveChanges();
                    TempData["SuccessMessage"] = "Solicitarea a fost retrimisă cu succes!";
                }
            }
            return RedirectToAction("Details", new { id = id });
        }

        // POST: Courses/ApproveRequest
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

                request.IsPending = false;
                request.IsApproved = true;
                request.ApprovalDate = DateTime.Now;

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
                TempData["SuccessMessage"] = "Cererea a fost aprobată!";
            }
            return RedirectToAction("Index", "Manage");
        }

        // POST: Courses/RejectRequest
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
                TempData["SuccessMessage"] = "Cererea a fost respinsă.";
            }
            return RedirectToAction("Index", "Manage");
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

            var currentUserId = User.Identity.GetUserId();
            var enrolledIds = course.EnrolledStudents.Select(s => s.Id).ToList();

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

            if (progressModel.Lessons.Count > 0)
            {
                double completed = progressModel.Lessons.Count(l => l.IsCompleted);
                progressModel.ProgressPercentage = (completed / progressModel.Lessons.Count) * 100;
            }

            var model = new CourseDetailsViewModel
            {
                Course = course,
                Progress = progressModel,
                IsEnrolled = enrolledIds.Contains(currentUserId),
                HasPendingRequest = db.EnrollmentRequests.Any(r => r.CourseId == id && r.StudentId == currentUserId)
            };

            ViewBag.EnrollmentRequest = db.EnrollmentRequests.FirstOrDefault(r => r.CourseId == id && r.StudentId == currentUserId);
            var studentRoleId = db.Roles.FirstOrDefault(r => r.Name == "Student")?.Id;

            ViewBag.RawStudentList = db.Users
                        .Where(u => u.Roles.Any(r => r.RoleId == studentRoleId))
                        .Where(u => u.Id != course.TeacherId)
                        .Where(u => !enrolledIds.Contains(u.Id))
                        .ToList();

            return View(model);
        }

        // POST: Courses/DeleteStudent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteStudent(int courseId, string studentId)
        {
            var course = db.Courses.Include(c => c.EnrolledStudents).FirstOrDefault(c => c.Id == courseId);
            var student = db.Users.Find(studentId);

            if (course != null && student != null)
            {
                course.EnrolledStudents.Remove(student);
                var req = db.EnrollmentRequests.FirstOrDefault(r => r.CourseId == courseId && r.StudentId == studentId);
                if (req != null) db.EnrollmentRequests.Remove(req);

                db.SaveChanges();
                TempData["SuccessMessage"] = "Elev eliminat cu succes.";
            }
            return RedirectToAction("Details", new { id = courseId });
        }

        // POST: Courses/AddStudent 
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Professor, Admin")]
        public ActionResult AddStudent(int courseId, string studentEmail)
        {
            var course = db.Courses.Include(c => c.EnrolledStudents).FirstOrDefault(c => c.Id == courseId);
            var student = db.Users.FirstOrDefault(u => u.Email == studentEmail);

            if (course != null && student != null)
            {
                if (!course.EnrolledStudents.Any(s => s.Id == student.Id))
                {
                    course.EnrolledStudents.Add(student);
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
                    TempData["SuccessMessage"] = "Studentul a fost înscris!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Studentul este deja înscris.";
                }
            }
            return RedirectToAction("Details", new { id = courseId });
        }

        // POST: Courses/Chat 
        [HttpPost]
        public async Task<ActionResult> Chat(string message)
        {
            var courses = db.Courses.Include(c => c.Lessons).ToList();

            string context = "Baza de date cursuri: ";
            foreach (var c in courses)
            {
                string lessonsSummary = "";
                foreach (var l in c.Lessons)
                {
                    string cleanContent = Regex.Replace(l.Content ?? "", "<.*?>", string.Empty);
                    lessonsSummary += $"[Lecția: {l.Title}, Detalii: {cleanContent.Substring(0, Math.Min(cleanContent.Length, 100))}...] ";
                }
                context += $"ID: {c.Id} | Curs: {c.Title} | Descriere: {c.Description} | {lessonsSummary}; ";
            }

            var aiService = new AIService();
            var responseFromAi = await aiService.GetCourseRecommendationAsync(message, context);

            string digits = new string(responseFromAi.Where(char.IsDigit).ToArray());

            int courseId = 0;
            if (!string.IsNullOrEmpty(digits))
            {
                int.TryParse(digits, out courseId);
            }

            var course = db.Courses.Find(courseId);

            return Json(new
            {
                reply = course != null ? "Îți recomand: " + course.Title : "Nu am găsit un curs relevant.",
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