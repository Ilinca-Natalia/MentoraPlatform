using System;
using System.Collections.Generic;

namespace MentoraPlatform.Models
{
    // Acest model va împacheta cursul și informațiile despre progres
    public class CourseDetailsViewModel
    {
        public Course Course { get; set; }
        public CourseProgressViewModel Progress { get; set; }
        public bool IsEnrolled { get; set; }
        public bool HasPendingRequest { get; set; }
    }

    public class CourseProgressViewModel
    {
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }
        public double ProgressPercentage { get; set; }
        public List<LessonStatusViewModel> Lessons { get; set; }
    }

    public class LessonStatusViewModel
    {
        public int LessonId { get; set; }
        public string Title { get; set; }
        public bool IsCompleted { get; set; }
    }
}