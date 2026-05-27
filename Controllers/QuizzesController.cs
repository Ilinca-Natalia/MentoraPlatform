using MentoraPlatform.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace MentoraPlatform.Controllers
{
    [Authorize]
    public class QuizzesController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // 1. Vizualizarea testului de către student
        public ActionResult TakeQuiz(int id)
        {
            // Încărcăm Quiz-ul cu toate întrebările și variantele de răspuns
            var quiz = db.Quizzes
                         .Include(q => q.Questions.Select(ques => ques.Choices))
                         .FirstOrDefault(q => q.Id == id);

            if (quiz == null) return HttpNotFound();

            return View(quiz);
        }

        // 2. Procesarea răspunsurilor și calcularea notei
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitQuiz(int quizId, FormCollection form)
        {
            var quiz = db.Quizzes.Include(q => q.Questions.Select(ques => ques.Choices))
                                 .FirstOrDefault(q => q.Id == quizId);

            if (quiz == null) return HttpNotFound();

            int correctAnswers = 0;
            int totalQuestions = quiz.Questions.Count;

            foreach (var question in quiz.Questions)
            {
                // Luăm ID-ul variantei selectate de student pentru fiecare întrebare
                string fieldName = "question_" + question.Id;
                string selectedChoiceIdStr = form[fieldName];

                if (!string.IsNullOrEmpty(selectedChoiceIdStr))
                {
                    int selectedChoiceId = int.Parse(selectedChoiceIdStr);
                    var selectedChoice = question.Choices.FirstOrDefault(c => c.Id == selectedChoiceId);

                    if (selectedChoice != null && selectedChoice.IsCorrect)
                    {
                        correctAnswers++;
                    }
                }
            }

            // Calculăm scorul (ex: de la 1 la 10)
            double score = (totalQuestions > 0) ? Math.Round(((double)correctAnswers / totalQuestions) * 10, 2) : 0;

            // Salvăm rezultatul în baza de date
            var result = new QuizResult
            {
                QuizId = quizId,
                StudentId = User.Identity.GetUserId(),
                Score = score,
                DateTaken = DateTime.Now
            };

            db.QuizResults.Add(result);
            db.SaveChanges();

            // Trimitem studentul la o pagină de rezultate
            return RedirectToAction("QuizResult", new { id = result.Id });
        }

        public ActionResult QuizResult(int id)
        {
            var result = db.QuizResults.Include(r => r.Quiz).FirstOrDefault(r => r.Id == id);
            return View(result);
        }

        [HttpPost]
        [Authorize(Roles = "Professor, Admin")]
        public async Task<ActionResult> PreviewGeneratedQuiz(int lessonId)
        {
            var lesson = db.Lessons.Include(l => l.Course).FirstOrDefault(l => l.Id == lessonId);
            if (lesson == null) return HttpNotFound();

            var aiService = new Services.AIService();
            // Trimitem textul lecției către AI
            var viewModel = await aiService.GenerateQuizAsync(lesson.Content);

            viewModel.LessonId = lessonId;
            viewModel.CourseTitle = lesson.Course.Title;

            return View(viewModel); // Aceasta va fi pagina unde profesorul editează
        }
        [HttpPost]
        [Authorize(Roles = "Professor, Admin")]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmAndSaveQuiz(QuizPreviewViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. Identificăm cursul de care aparține lecția
                var lesson = db.Lessons.Find(model.LessonId);
                if (lesson == null) return HttpNotFound();

                // 2. Creăm obiectul principal Quiz
                var quiz = new Quiz
                {
                    Title = model.QuizTitle,
                    CourseId = lesson.CourseId
                };
                db.Quizzes.Add(quiz);

                // 3. Parcurgem întrebările venite din formular
                foreach (var qPreview in model.Questions)
                {
                    if (string.IsNullOrWhiteSpace(qPreview.Text)) continue;

                    var question = new Question
                    {
                        Text = qPreview.Text,
                        Quiz = quiz
                    };
                    db.Questions.Add(question);

                    // 4. Parcurgem variantele de răspuns pentru fiecare întrebare
                    foreach (var cPreview in qPreview.Choices)
                    {
                        if (string.IsNullOrWhiteSpace(cPreview.Text)) continue;

                        var choice = new Choice
                        {
                            AnswerText = cPreview.Text,
                            IsCorrect = cPreview.IsCorrect,
                            Question = question
                        };
                        db.Choices.Add(choice);
                    }
                }

                db.SaveChanges();

                // Redirecționăm profesorul înapoi la curs cu un mesaj de succes
                TempData["SuccessMessage"] = "Testul a fost generat și salvat cu succes!";
                return RedirectToAction("Details", "Courses", new { id = lesson.CourseId });
            }

            // Dacă ceva nu a mers bine, reîncărcăm pagina de preview
            return View("PreviewGeneratedQuiz", model);
        }

        // GET: Quizzes sau Quizzes?courseId=5
        public ActionResult Index(int? courseId)
        {
            IQueryable<Quiz> quizzes = db.Quizzes.Include(q => q.Course);

            // Dacă venim din pagina de curs, filtrăm. 
            // Dacă venim din Navbar (courseId e null), le arătăm pe toate.
            if (courseId.HasValue)
            {
                quizzes = quizzes.Where(q => q.CourseId == courseId.Value);
            }

            return View(quizzes.ToList());
        }
        // GET: Quizzes/All
        public ActionResult All()
        {
            // Luăm toate testele și includem cursurile lor
            var allQuizzes = db.Quizzes.Include(q => q.Course).ToList();
            return View("Index", allQuizzes); // Refolosim View-ul Index existent
        }

        // GET: Quizzes/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            // Încărcăm tot graful: Quiz -> Întrebări -> Variante
            var quiz = db.Quizzes
                         .Include(q => q.Course)
                         .Include(q => q.Questions.Select(ques => ques.Choices))
                         .FirstOrDefault(q => q.Id == id);

            if (quiz == null) return HttpNotFound();

            if (quiz.Course.TeacherId != User.Identity.GetUserId() && !User.IsInRole("Admin"))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            return View(quiz);
        }

        // POST: Quizzes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Quiz quiz, FormCollection form)
        {
            // 1. Luăm obiectul din DB cu tot cu întrebări ca să evităm eroarea de "Attaching"
            var dbQuiz = db.Quizzes
                           .Include(q => q.Course)
                           .Include(q => q.Questions.Select(ques => ques.Choices))
                           .FirstOrDefault(q => q.Id == quiz.Id);

            if (dbQuiz == null) return HttpNotFound();

            // 2. Verificăm securitatea
            if (dbQuiz.Course.TeacherId != User.Identity.GetUserId() && !User.IsInRole("Admin"))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            // 3. Actualizăm Titlul Quiz-ului
            dbQuiz.Title = quiz.Title;

            // 4. Actualizăm Întrebările și Variantele din FormCollection
            foreach (var question in dbQuiz.Questions)
            {
                // Actualizăm textul întrebării
                string qKey = "question_" + question.Id;
                if (!string.IsNullOrEmpty(form[qKey]))
                {
                    question.Text = form[qKey];
                }

                foreach (var choice in question.Choices)
                {
                    // Actualizăm textul variantei
                    string cKey = "choice_" + choice.Id;
                    if (!string.IsNullOrEmpty(form[cKey]))
                    {
                        choice.AnswerText = form[cKey];
                    }

                    // Actualizăm IsCorrect (checkbox)
                    string correctKey = "correct_" + choice.Id;
                    choice.IsCorrect = (form[correctKey] != null && form[correctKey].Contains("true"));
                }
            }

            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // GET: Quizzes/Delete/5
        [Authorize(Roles = "Professor, Admin")]
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var quiz = db.Quizzes.Include(q => q.Course).FirstOrDefault(q => q.Id == id);
            if (quiz == null) return HttpNotFound();

            // VERIFICARE: Doar proprietarul cursului sau Adminul are voie
            if (quiz.Course.TeacherId != User.Identity.GetUserId() && !User.IsInRole("Admin"))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            return View(quiz);
        }
       

        // POST: Quizzes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Professor, Admin")]
        public ActionResult DeleteConfirmed(int id)
        {
            Quiz quiz = db.Quizzes.Include(q => q.Course).FirstOrDefault(q => q.Id == id);

            // Verificăm proprietarul înainte de ștergere
            if (quiz.Course.TeacherId != User.Identity.GetUserId() && !User.IsInRole("Admin"))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            db.Quizzes.Remove(quiz);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}