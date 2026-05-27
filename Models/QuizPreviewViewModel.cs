using System.Collections.Generic;

namespace MentoraPlatform.Models
{
    public class QuizPreviewViewModel
    {
        public int LessonId { get; set; }
        public string CourseTitle { get; set; }
        public string QuizTitle { get; set; }
        public List<QuestionPreview> Questions { get; set; } = new List<QuestionPreview>();
    }

    public class QuestionPreview
    {
        public string Text { get; set; }
        public List<ChoicePreview> Choices { get; set; } = new List<ChoicePreview>();
    }

    public class ChoicePreview
    {
        public string Text { get; set; }
        public bool IsCorrect { get; set; }
    }
}