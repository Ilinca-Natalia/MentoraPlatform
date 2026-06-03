using Microsoft.AspNetCore.Identity;

public class RomanianIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => new IdentityError { Code = nameof(DefaultError), Description = "A apărut o eroare necunoscută." };

    public override IdentityError PasswordTooShort(int length) =>
        new IdentityError { Code = nameof(PasswordTooShort), Description = $"Parola trebuie să aibă cel puțin {length} caractere." };

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        new IdentityError { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Parola trebuie să conțină cel puțin un caracter special." };

    public override IdentityError PasswordRequiresDigit() =>
        new IdentityError { Code = nameof(PasswordRequiresDigit), Description = "Parola trebuie să conțină cel puțin o cifră." };

    public override IdentityError PasswordRequiresLower() =>
        new IdentityError { Code = nameof(PasswordRequiresLower), Description = "Parola trebuie să conțină cel puțin o literă mică." };

    public override IdentityError PasswordRequiresUpper() =>
        new IdentityError { Code = nameof(PasswordRequiresUpper), Description = "Parola trebuie să conțină cel puțin o literă mare." };

    public override IdentityError DuplicateUserName(string userName) =>
        new IdentityError { Code = nameof(DuplicateUserName), Description = $"Numele de utilizator '{userName}' este deja utilizat." };

    public override IdentityError DuplicateEmail(string email) =>
        new IdentityError { Code = nameof(DuplicateEmail), Description = $"Adresa de e-mail '{email}' este deja utilizată." };

    public override IdentityError InvalidEmail(string email) =>
        new IdentityError { Code = nameof(InvalidEmail), Description = $"Adresa de e-mail '{email}' este invalidă." };
}