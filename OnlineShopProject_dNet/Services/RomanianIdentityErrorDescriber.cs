using Microsoft.AspNetCore.Identity;

namespace OnlineShopProject_dNet.Services
{
    /// <summary>
    /// Descriptor de erori Identity personalizat pentru limba romana
    /// </summary>
    public class RomanianIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError DefaultError() =>
            new() { Code = nameof(DefaultError), Description = "A aparut o eroare neasteptata." };

        public override IdentityError ConcurrencyFailure() =>
            new() { Code = nameof(ConcurrencyFailure), Description = "Conflict de concurenta, obiectul a fost modificat." };

        public override IdentityError PasswordMismatch() =>
            new() { Code = nameof(PasswordMismatch), Description = "Parola incorecta." };

        public override IdentityError InvalidToken() =>
            new() { Code = nameof(InvalidToken), Description = "Token invalid." };

        public override IdentityError LoginAlreadyAssociated() =>
            new() { Code = nameof(LoginAlreadyAssociated), Description = "Un utilizator cu acest email exista deja." };

        public override IdentityError InvalidUserName(string? userName) =>
            new() { Code = nameof(InvalidUserName), Description = $"Email '{userName ?? "unknown"}' este invalid, poate contine doar litere sau cifre." };

        public override IdentityError InvalidEmail(string? email) =>
            new() { Code = nameof(InvalidEmail), Description = $"Email '{email ?? "unknown"}' este invalid." };

        public override IdentityError DuplicateUserName(string userName) =>
            new() { Code = nameof(DuplicateUserName), Description = $"Email '{userName}' este deja folosit." };

        public override IdentityError DuplicateEmail(string email) =>
            new() { Code = nameof(DuplicateEmail), Description = $"Email '{email}' este deja folosit." };

        public override IdentityError InvalidRoleName(string? role) =>
            new() { Code = nameof(InvalidRoleName), Description = $"Rolul '{role ?? "unknown"}' este invalid." };

        public override IdentityError DuplicateRoleName(string role) =>
            new() { Code = nameof(DuplicateRoleName), Description = $"Rolul '{role}' este deja folosit." };

        public override IdentityError UserAlreadyHasPassword() =>
            new() { Code = nameof(UserAlreadyHasPassword), Description = "Utilizatorul are deja o parola setata." };

        public override IdentityError UserLockoutNotEnabled() =>
            new() { Code = nameof(UserLockoutNotEnabled), Description = "Blocarea nu este activata pentru acest utilizator." };

        public override IdentityError UserAlreadyInRole(string role) =>
            new() { Code = nameof(UserAlreadyInRole), Description = $"Utilizatorul are deja rolul '{role}'." };

        public override IdentityError UserNotInRole(string role) =>
            new() { Code = nameof(UserNotInRole), Description = $"Utilizatorul nu are rolul '{role}'." };

        public override IdentityError PasswordTooShort(int length) =>
            new() { Code = nameof(PasswordTooShort), Description = $"Parola trebuie sa aiba cel putin {length} caractere." };

        public override IdentityError PasswordRequiresNonAlphanumeric() =>
            new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Parola trebuie sa contina cel putin un caracter special (!@#$%^&*)." };

        public override IdentityError PasswordRequiresDigit() =>
            new() { Code = nameof(PasswordRequiresDigit), Description = "Parola trebuie sa contina cel putin o cifra ('0'-'9')." };

        public override IdentityError PasswordRequiresLower() =>
            new() { Code = nameof(PasswordRequiresLower), Description = "Parola trebuie sa contina cel putin o litera mica ('a'-'z')." };

        public override IdentityError PasswordRequiresUpper() =>
            new() { Code = nameof(PasswordRequiresUpper), Description = "Parola trebuie sa contina cel putin o litera mare ('A'-'Z')." };

        public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
            new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"Parola trebuie sa aiba cel putin {uniqueChars} caractere unice." };

        public override IdentityError RecoveryCodeRedemptionFailed() =>
            new() { Code = nameof(RecoveryCodeRedemptionFailed), Description = "Recuperarea a esuat." };
    }
}
