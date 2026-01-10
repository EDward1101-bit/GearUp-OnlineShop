using Microsoft.AspNetCore.Identity;

namespace OnlineShopProject_dNet.Services
{
    /// <summary>
    /// Descriptor de erori Identity personalizat pentru limba român?
    /// </summary>
    public class RomanianIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError DefaultError() =>
            new() { Code = nameof(DefaultError), Description = "A ap?rut o eroare nea?teptat?." };

        public override IdentityError ConcurrencyFailure() =>
            new() { Code = nameof(ConcurrencyFailure), Description = "Conflict de concuren??, obiectul a fost modificat." };

        public override IdentityError PasswordMismatch() =>
            new() { Code = nameof(PasswordMismatch), Description = "Parol? incorect?." };

        public override IdentityError InvalidToken() =>
            new() { Code = nameof(InvalidToken), Description = "Token invalid." };

        public override IdentityError LoginAlreadyAssociated() =>
            new() { Code = nameof(LoginAlreadyAssociated), Description = "Un utilizator cu acest email exist? deja." };

        public override IdentityError InvalidUserName(string? userName) =>
            new() { Code = nameof(InvalidUserName), Description = $"Email '{userName ?? "unknown"}' este invalid, poate con?ine doar litere sau cifre." };

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
            new() { Code = nameof(UserAlreadyHasPassword), Description = "Utilizatorul are deja o parol? setat?." };

        public override IdentityError UserLockoutNotEnabled() =>
            new() { Code = nameof(UserLockoutNotEnabled), Description = "Blocarea nu este activat? pentru acest utilizator." };

        public override IdentityError UserAlreadyInRole(string role) =>
            new() { Code = nameof(UserAlreadyInRole), Description = $"Utilizatorul are deja rolul '{role}'." };

        public override IdentityError UserNotInRole(string role) =>
            new() { Code = nameof(UserNotInRole), Description = $"Utilizatorul nu are rolul '{role}'." };

        public override IdentityError PasswordTooShort(int length) =>
            new() { Code = nameof(PasswordTooShort), Description = $"Parola trebuie s? aib? cel pu?in {length} caractere." };

        public override IdentityError PasswordRequiresNonAlphanumeric() =>
            new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Parola trebuie s? con?in? cel pu?in un caracter special (!@#$%^&*)." };

        public override IdentityError PasswordRequiresDigit() =>
            new() { Code = nameof(PasswordRequiresDigit), Description = "Parola trebuie s? con?in? cel pu?in o cifr? ('0'-'9')." };

        public override IdentityError PasswordRequiresLower() =>
            new() { Code = nameof(PasswordRequiresLower), Description = "Parola trebuie s? con?in? cel pu?in o liter? mic? ('a'-'z')." };

        public override IdentityError PasswordRequiresUpper() =>
            new() { Code = nameof(PasswordRequiresUpper), Description = "Parola trebuie s? con?in? cel pu?in o liter? mare ('A'-'Z')." };

        public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
            new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"Parola trebuie s? aib? cel pu?in {uniqueChars} caractere unice." };

        public override IdentityError RecoveryCodeRedemptionFailed() =>
            new() { Code = nameof(RecoveryCodeRedemptionFailed), Description = "Recuperarea a e?uat." };
    }
}

