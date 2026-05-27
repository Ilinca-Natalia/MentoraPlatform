using System;
using System.ComponentModel.DataAnnotations;

namespace MentoraPlatform.Models
{
    public class CodeProject
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Titlu Proiect")]
        public string Title { get; set; }

        // Salvăm codul ca text lung
        [DataType(DataType.MultilineText)]
        public string HtmlCode { get; set; }

        [DataType(DataType.MultilineText)]
        public string CssCode { get; set; }

        [DataType(DataType.MultilineText)]
        public string JsCode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Relația cu utilizatorul
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }
    }
}