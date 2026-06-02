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

        // Codul sursă stocat ca text lung (NVARCHAR(MAX)) în baza de date SQL Server
        [DataType(DataType.MultilineText)]
        public string HtmlCode { get; set; }

        [DataType(DataType.MultilineText)]
        public string CssCode { get; set; }

        [DataType(DataType.MultilineText)]
        public string JsCode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Relația cu tabelul de utilizatori (Identity) pentru a asigura securitatea proiectelor
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }
    }
}