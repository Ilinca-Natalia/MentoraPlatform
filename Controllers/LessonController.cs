using MentoraPlatform.Models;
using Microsoft.AspNet.Identity; 
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
namespace MentoraPlatform.Controllers
{
    [Authorize]
    public class LessonsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Lessons/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            // ADĂUGĂM .Include(l => l.LessonAttachments) ca să vedem fișierele!
            var lesson = db.Lessons
                           .Include(l => l.Course)
                           .Include(l => l.LessonAttachments)
                           .FirstOrDefault(l => l.Id == id);

            if (lesson == null) return HttpNotFound();

            // Trimitem către View starea de finalizare pentru studentul logat
            var userId = User.Identity.GetUserId();
            ViewBag.IsCompleted = db.UserLessonProgresses
                                    .Any(p => p.UserId == userId && p.LessonId == id);

            return View(lesson);
        }

        // GET: Lessons/Create?courseId=5
        [Authorize(Roles = "Professor, Admin")]
        public ActionResult Create(int courseId)
        {
            var course = db.Courses.Find(courseId);
            if (course == null) return HttpNotFound();

            if (course.TeacherId != User.Identity.GetUserId() && !User.IsInRole("Admin"))
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            ViewBag.CourseTitle = course.Title;
            ViewBag.CourseId = courseId; // Trimitem ID-ul către View
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Professor, Admin")]
        public ActionResult Create([Bind(Include = "Title,Content,CourseId,VideoUrl")] Lesson lesson, IEnumerable<HttpPostedFileBase> files)
        {
            if (ModelState.IsValid)
            {
                // 1. Curățăm link-ul de YouTube pentru Embed
                if (!string.IsNullOrEmpty(lesson.VideoUrl))
                {
                    lesson.VideoUrl = lesson.VideoUrl.Replace("watch?v=", "embed/");
                }

                // 2. ADĂUGĂM lecția în baza de date (Această linie lipsea probabil!)
                db.Lessons.Add(lesson);
                db.SaveChanges(); // Salvăm prima dată pentru a genera ID-ul lecției

                // 3. Procesăm fișierele încărcate (doar dacă există)
                if (files != null)
                {
                    string uploadDir = "~/Uploads/Lessons/";
                    string physicalPath = Server.MapPath(uploadDir);

                    // Creăm folderul dacă nu există
                    if (!Directory.Exists(physicalPath))
                    {
                        Directory.CreateDirectory(physicalPath);
                    }

                    foreach (var file in files)
                    {
                        if (file != null && file.ContentLength > 0)
                        {
                            string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                            string path = Path.Combine(physicalPath, fileName);
                            file.SaveAs(path);

                            var attachment = new LessonAttachment
                            {
                                FileName = file.FileName,
                                FilePath = "/Uploads/Lessons/" + fileName,
                                LessonId = lesson.Id, // Folosim ID-ul lecției proaspăt salvate
                                FileType = Path.GetExtension(file.FileName)
                            };
                            db.LessonAttachments.Add(attachment);
                        }
                    }
                    db.SaveChanges(); // Salvăm atașamentele
                }

                return RedirectToAction("Details", "Courses", new { id = lesson.CourseId });
            }

            // DACĂ MODELUL NU ESTE VALID, repopulăm datele pentru View
            var course = db.Courses.Find(lesson.CourseId);
            ViewBag.CourseTitle = course?.Title;
            ViewBag.CourseId = lesson.CourseId;

            return View(lesson);
        }

        // GET: Lessons/Edit/5
        [Authorize(Roles = "Professor, Admin")]
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var lesson = db.Lessons.Include(l => l.Course).FirstOrDefault(l => l.Id == id);
            if (lesson == null) return HttpNotFound();

            // Verificăm dacă profesorul deține cursul din care face parte lecția
            if (lesson.Course.TeacherId != User.Identity.GetUserId() && !User.IsInRole("Admin"))
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            return View(lesson);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Professor, Admin")]
        // AM ADĂUGAT VideoUrl în listă!
        public ActionResult Edit([Bind(Include = "Id,Title,Content,CourseId,VideoUrl")] Lesson lesson)
        {
            if (ModelState.IsValid)
            {
                // Curățăm link-ul de YouTube și la editare!
                if (!string.IsNullOrEmpty(lesson.VideoUrl))
                {
                    lesson.VideoUrl = lesson.VideoUrl.Replace("watch?v=", "embed/");
                }

                db.Entry(lesson).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Details", "Courses", new { id = lesson.CourseId });
            }
            return View(lesson);
        }

        // GET: Lessons/Delete/5
        [Authorize(Roles = "Professor, Admin")]
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var lesson = db.Lessons.Include(l => l.Course).FirstOrDefault(l => l.Id == id);
            if (lesson == null) return HttpNotFound();

            if (lesson.Course.TeacherId != User.Identity.GetUserId() && !User.IsInRole("Admin"))
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            return View(lesson);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Professor, Admin")]
        public ActionResult DeleteConfirmed(int id)
        {
            Lesson lesson = db.Lessons.Find(id);
            int courseId = lesson.CourseId;
            db.Lessons.Remove(lesson);
            db.SaveChanges();
            return RedirectToAction("Details", "Courses", new { id = courseId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkComplete(int id)
        {
            var userId = User.Identity.GetUserId();
            var lesson = db.Lessons.Find(id);

            // Verificăm dacă nu a fost deja marcată
            var existing = db.UserLessonProgresses.FirstOrDefault(p => p.UserId == userId && p.LessonId == id);
            if (existing == null)
            {
                db.UserLessonProgresses.Add(new UserLessonProgress
                {
                    UserId = userId,
                    LessonId = id,
                    CompletedDate = DateTime.Now
                });
                db.SaveChanges();
            }

            return RedirectToAction("Details", new { id = id });
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}