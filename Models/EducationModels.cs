using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MentoraPlatform.Models
{
   
        public class Course
        {
            [Key]
            public int Id { get; set; }

            [Required(ErrorMessage = "Titlul este obligatoriu")]
            [Display(Name = "Titlu Curs")]
            public string Title { get; set; }

            [Display(Name = "Descriere")]
            [DataType(DataType.MultilineText)]
            public string Description { get; set; }

            public DateTime CreatedAt { get; set; } = DateTime.Now;

            public string TeacherId { get; set; }
            [ForeignKey("TeacherId")]
            public virtual ApplicationUser Teacher { get; set; }

            public virtual ICollection<Lesson> Lessons { get; set; }
            public virtual ICollection<ApplicationUser> EnrolledStudents { get; set; } = new List<ApplicationUser>();

            // Un curs poate avea mai multe Quiz-uri (de exemplu, unul după fiecare capitol)
            public virtual ICollection<Quiz> Quizzes { get; set; }
        }

        public class Lesson
        {
            [Key]
            public int Id { get; set; }

            [Required]
            public string Title { get; set; }

            [AllowHtml]
            public string Content { get; set; }

            [Display(Name = "URL Video (YouTube)")]
            public string VideoUrl { get; set; }

            public int CourseId { get; set; }
            [ForeignKey("CourseId")]
            public virtual Course Course { get; set; }

            public virtual ICollection<LessonAttachment> LessonAttachments { get; set; } = new List<LessonAttachment>();
        }

        public class LessonAttachment
        {
            [Key]
            public int Id { get; set; }
            public string FileName { get; set; }
            public string FilePath { get; set; }
            public string FileType { get; set; }

            public int LessonId { get; set; }
            [ForeignKey("LessonId")]
            public virtual Lesson Lesson { get; set; }
        }

        // --- NOILE MODELE PENTRU QUIZ AI ---

        public class Quiz
        {
            [Key]
            public int Id { get; set; }

            [Required]
            [Display(Name = "Titlu Quiz")]
            public string Title { get; set; }

            // Putem lega Quiz-ul de un Curs întreg sau de o Lecție specifică
            public int CourseId { get; set; }
            [ForeignKey("CourseId")]
            public virtual Course Course { get; set; }

            public virtual ICollection<Question> Questions { get; set; }
        }

        public class Question
        {
            [Key]
            public int Id { get; set; }

            [Required]
            [Display(Name = "Întrebare")]
            public string Text { get; set; }

            public int QuizId { get; set; }
            [ForeignKey("QuizId")]
            public virtual Quiz Quiz { get; set; }

            public virtual ICollection<Choice> Choices { get; set; }
        }

        public class Choice
        {
            [Key]
            public int Id { get; set; }

            [Required]
            public string AnswerText { get; set; }

            public bool IsCorrect { get; set; }

            public int QuestionId { get; set; }
            [ForeignKey("QuestionId")]
            public virtual Question Question { get; set; }
        }

        public class QuizResult
        {
            [Key]
            public int Id { get; set; }

            public string StudentId { get; set; }
            [ForeignKey("StudentId")]
            public virtual ApplicationUser Student { get; set; }

            public int QuizId { get; set; }
            [ForeignKey("QuizId")]
            public virtual Quiz Quiz { get; set; }

            [Display(Name = "Scor")]
            public double Score { get; set; }

            [Display(Name = "Data Susținerii")]
            public DateTime DateTaken { get; set; } = DateTime.Now;
        }

        public class UserLessonProgress
        {
            [Key]
            public int Id { get; set; }

            public string UserId { get; set; }
            [ForeignKey("UserId")]
            public virtual ApplicationUser User { get; set; }

            public int LessonId { get; set; }
            [ForeignKey("LessonId")]
            public virtual Lesson Lesson { get; set; }

            public DateTime CompletedDate { get; set; } = DateTime.Now;
        }
}