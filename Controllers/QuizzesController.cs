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

        // GET: Quizzes/TakeQuiz/5 
        public ActionResult TakeQuiz(int id)
        {
            var quiz = db.Quizzes
                         .Include(q => q.Questions.Select(ques => ques.Choices))
                         .FirstOrDefault(q => q.Id == id);

            if (quiz == null) return HttpNotFound();

            return View(quiz);
        }

        // POST: Quizzes/SubmitQuiz
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

            double score = (totalQuestions > 0) ? Math.Round(((double)correctAnswers / totalQuestions) * 10, 2) : 0;

            var result = new QuizResult
            {
                QuizId = quizId,
                StudentId = User.Identity.GetUserId(),
                Score = score,
                DateTaken = DateTime.Now
            };

            db.QuizResults.Add(result);
            db.SaveChanges();

            return RedirectToAction("QuizResult", new { id = result.Id });
        }

        // GET: Quizzes/QuizResult/5 
        public ActionResult QuizResult(int id)
        {
            var result = db.QuizResults.Include(r => r.Quiz).FirstOrDefault(r => r.Id == id);
            return View(result);
        }

        // POST: Quizzes/PreviewGeneratedQuiz 
        [HttpPost]
        [Authorize(Roles = "Professor, Admin")]
        public async Task<ActionResult> PreviewGeneratedQuiz(int lessonId)
        {
            var lesson = db.Lessons.Include(l => l.Course).FirstOrDefault(l => l.Id == lessonId);
            if (lesson == null) return HttpNotFound();

            var aiService = new Services.AIService();
            
            var viewModel = await aiService.GenerateQuizAsync(lesson.Content);

            viewModel.LessonId = lessonId;
            viewModel.CourseTitle = lesson.Course.Title;

            return View(viewModel);
        }

        // POST: Quizzes/ConfirmAndSaveQuiz
        [HttpPost]
        [Authorize(Roles = "Professor, Admin")]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmAndSaveQuiz(QuizPreviewViewModel model)
        {
            if (ModelState.IsValid)
            {
                var lesson = db.Lessons.Find(model.LessonId);
                if (lesson == null) return HttpNotFound();

                var quiz = new Quiz
                {
                    Title = model.QuizTitle,
                    CourseId = lesson.CourseId
                };
                db.Quizzes.Add(quiz);

                foreach (var qPreview in model.Questions)
                {
                    if (string.IsNullOrWhiteSpace(qPreview.Text)) continue;

                    var question = new Question { Text = qPreview.Text, Quiz = quiz };
                    db.Questions.Add(question);

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
                TempData["SuccessMessage"] = "Testul a fost generat și salvat cu succes!";
                return RedirectToAction("Details", "Courses", new { id = lesson.CourseId });
            }
            return View("PreviewGeneratedQuiz", model);
        }

        // GET: Quizzes/Index 
        public ActionResult Index(int? courseId)
        {
            IQueryable<Quiz> quizzes = db.Quizzes.Include(q => q.Course);
            if (courseId.HasValue) quizzes = quizzes.Where(q => q.CourseId == courseId.Value);
            return View(quizzes.ToList());
        }

        // GET: Quizzes/All 
        public ActionResult All()
        {
            var allQuizzes = db.Quizzes.Include(q => q.Course).ToList();
            return View("Index", allQuizzes);
        }

        // GET: Quizzes/Edit/5 
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

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
            var dbQuiz = db.Quizzes
                           .Include(q => q.Course)
                           .Include(q => q.Questions.Select(ques => ques.Choices))
                           .FirstOrDefault(q => q.Id == quiz.Id);

            if (dbQuiz == null) return HttpNotFound();

            if (dbQuiz.Course.TeacherId != User.Identity.GetUserId() && !User.IsInRole("Admin"))
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            dbQuiz.Title = quiz.Title;

            foreach (var question in dbQuiz.Questions)
            {
                string qKey = "question_" + question.Id;
                if (!string.IsNullOrEmpty(form[qKey])) question.Text = form[qKey];

                foreach (var choice in question.Choices)
                {
                    string cKey = "choice_" + choice.Id;
                    if (!string.IsNullOrEmpty(form[cKey])) choice.AnswerText = form[cKey];

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

            if (quiz.Course.TeacherId != User.Identity.GetUserId() && !User.IsInRole("Admin"))
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            return View(quiz);
        }

        // POST: Quizzes/Delete/5 
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Professor, Admin")]
        public ActionResult DeleteConfirmed(int id)
        {
            Quiz quiz = db.Quizzes.Include(q => q.Course).FirstOrDefault(q => q.Id == id);
            if (quiz.Course.TeacherId != User.Identity.GetUserId() && !User.IsInRole("Admin"))
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

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