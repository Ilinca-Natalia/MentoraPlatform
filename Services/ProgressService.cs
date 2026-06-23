using System;
using System.Linq;
using MentoraPlatform.Models;

namespace MentoraPlatform.Services
{
    public class ProgressService
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        // Marchează o lecție ca finalizată pentru un utilizator
        public void MarkLessonAsCompleted(string userId, int lessonId)
        {
            if (!_db.UserLessonProgresses.Any(p => p.UserId == userId && p.LessonId == lessonId))
            {
                var progress = new UserLessonProgress
                {
                    UserId = userId,
                    LessonId = lessonId,
                    CompletedDate = DateTime.Now
                };
                _db.UserLessonProgresses.Add(progress);
                _db.SaveChanges();
            }
        }

        // Calculează procentajul de progres pentru un curs specific
        public double GetCourseProgress(string userId, int courseId)
        {
            var totalLessons = _db.Lessons.Count(l => l.CourseId == courseId);
            if (totalLessons == 0) return 0;

            var completedLessons = _db.UserLessonProgresses
                .Count(p => p.UserId == userId && p.Lesson.CourseId == courseId);

            return (double)completedLessons / totalLessons * 100;
        }
        public DateTime? GetEnrollmentDate(string userId, int courseId)
        {
            return _db.EnrollmentRequests
                .FirstOrDefault(r => r.StudentId == userId && r.CourseId == courseId && r.IsApproved)
                ?.ApprovalDate;
        }
        public StudentRiskViewModel GetStudentRisk(string userId, int courseId)
        {
            // 1. Progresul actual (folosind metoda ta)
            double progress = GetCourseProgress(userId, courseId);

            // 2. Ultima activitate (zile de la ultima lecție)
            var lastActivity = _db.UserLessonProgresses
                                 .Where(p => p.UserId == userId && p.Lesson.CourseId == courseId)
                                 .OrderByDescending(p => p.CompletedDate)
                                 .Select(p => p.CompletedDate)
                                 .FirstOrDefault();

            int days = lastActivity != default ? (DateTime.Now - lastActivity).Days : 30;

            // 3. Media notelor
            var scores = _db.QuizResults.Where(r => r.StudentId == userId && r.Quiz.CourseId == courseId);
            double avg = scores.Any() ? scores.Average(r => r.Score) : 0;

            return new StudentRiskViewModel
            {
                StudentId = userId,
                ProgressPercentage = progress,
                AverageScore = avg,
                DaysSinceLastActivity = days
            };
        }
    }
}