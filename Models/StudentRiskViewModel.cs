using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MentoraPlatform.Models
{
    public class StudentRiskViewModel
    {
        
        public string StudentId { get; set; }
        public string FullName { get; set; }
        public double ProgressPercentage { get; set; }
        public double AverageScore { get; set; }
        public int DaysSinceLastActivity { get; set; }

        // Logica de calcul a riscului (IsAtRisk devine true dacă se îndeplinește oricare condiție)
        public bool IsAtRisk => ProgressPercentage < 20 || AverageScore < 4 || DaysSinceLastActivity > 14;
    }
}