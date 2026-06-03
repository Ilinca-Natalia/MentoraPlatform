using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MentoraPlatform.Models
{
    public class ForgotViewModel
    {
        [Required(ErrorMessage = "Adresa de email este obligatorie.")]
        [EmailAddress(ErrorMessage = "Formatul adresei de email nu este valid.")]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Adresa de email este obligatorie.")]
        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Formatul adresei de email nu este valid.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Parola este obligatorie.")]
        [DataType(DataType.Password)]
        [Display(Name = "Parolă")]
        public string Password { get; set; }

        [Display(Name = "Ține-mă minte?")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Email-ul este obligatoriu.")]
        [EmailAddress(ErrorMessage = "Formatul adresei de email nu este valid.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Prenumele este obligatoriu.")]
        [Display(Name = "Prenume")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Numele este obligatoriu.")]
        [Display(Name = "Nume")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Parola este obligatorie.")]
        [StringLength(100, ErrorMessage = "Parola trebuie să aibă cel puțin {2} caractere.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Parolă")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmă parola")]
        [Compare("Password", ErrorMessage = "Parola și confirmarea parolei nu se potrivesc.")]
        public string ConfirmPassword { get; set; }
    }

    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "Email-ul este obligatoriu.")]
        [EmailAddress(ErrorMessage = "Formatul adresei de email nu este valid.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Parola este obligatorie.")]
        [StringLength(100, ErrorMessage = "Parola trebuie să aibă cel puțin {2} caractere.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Parolă")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmă parola")]
        [Compare("Password", ErrorMessage = "Parola și confirmarea parolei nu se potrivesc.")]
        public string ConfirmPassword { get; set; }

        public string Code { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email-ul este obligatoriu.")]
        [EmailAddress(ErrorMessage = "Formatul adresei de email nu este valid.")]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }

    public class ExternalLoginListViewModel
    {
        public string ReturnUrl { get; set; }
    }
}