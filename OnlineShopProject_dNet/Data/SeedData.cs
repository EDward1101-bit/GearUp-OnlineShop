using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineShopProject_dNet.Models;

namespace OnlineShopProject_dNet.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // Verificăm dacă există deja roluri în baza de date ca să nu le dublăm
                if (context.Roles.Any()) return;

               
                const string ADMIN_ROLE_ID = "5c5e174e-3b0e-446f-86af-483d56fd7210";
                const string PROPOSER_ROLE_ID = "5c5e174e-3b0e-446f-86af-483d56fd7211";
                const string USER_ROLE_ID = "5c5e174e-3b0e-446f-86af-483d56fd7212";

                const string ADMIN_USER_ID = "9e445865-a24d-4543-a6c6-9443d048cdb9";
                const string PROPOSER_USER_ID = "9e445865-a24d-4543-a6c6-9443d048cdb8";
                const string NORMAL_USER_ID = "9e445865-a24d-4543-a6c6-9443d048cdb7";

                // --- ADĂUGAREA ROLURILOR ---
                context.Roles.AddRange(
                    new IdentityRole { Id = ADMIN_ROLE_ID, Name = "Admin", NormalizedName = "ADMIN" },
                    new IdentityRole { Id = PROPOSER_ROLE_ID, Name = "Proposer", NormalizedName = "PROPOSER" },
                    new IdentityRole { Id = USER_ROLE_ID, Name = "User", NormalizedName = "USER" }
                );

                // --- ADĂUGAREA USERILOR CU PAROLA HASH-UITĂ ---
                var hasher = new PasswordHasher<ApplicationUser>();

                // User ADMIN
                var adminUser = new ApplicationUser
                {
                    Id = ADMIN_USER_ID,
                    UserName = "admin@test.com",
                    NormalizedUserName = "ADMIN@TEST.COM",
                    Email = "admin@test.com",
                    NormalizedEmail = "ADMIN@TEST.COM",
                    EmailConfirmed = true,
                    PasswordHash = hasher.HashPassword(null, "Admin123!"), // Parola explicita
                    SecurityStamp = Guid.NewGuid().ToString(),
                    FirstName = "Admin",
                    LastName = "Sistem"
                };

                // User PROPOSER (Colaborator)
                var proposerUser = new ApplicationUser
                {
                    Id = PROPOSER_USER_ID,
                    UserName = "proposer@test.com",
                    NormalizedUserName = "PROPOSER@TEST.COM",
                    Email = "proposer@test.com",
                    NormalizedEmail = "PROPOSER@TEST.COM",
                    EmailConfirmed = true,
                    PasswordHash = hasher.HashPassword(null, "Proposer123!"),
                    SecurityStamp = Guid.NewGuid().ToString(),
                    FirstName = "Dan",
                    LastName = "Propunator"
                };

                // User NORMAL (Vizitator Inregistrat)
                var normalUser = new ApplicationUser
                {
                    Id = NORMAL_USER_ID,
                    UserName = "user@test.com",
                    NormalizedUserName = "USER@TEST.COM",
                    Email = "user@test.com",
                    NormalizedEmail = "USER@TEST.COM",
                    EmailConfirmed = true,
                    PasswordHash = hasher.HashPassword(null, "User123!"),
                    SecurityStamp = Guid.NewGuid().ToString(),
                    FirstName = "Ion",
                    LastName = "Userescu"
                };

                context.Users.AddRange(adminUser, proposerUser, normalUser);

                
                // Legăm ID-ul userului de ID-ul rolului
                context.UserRoles.AddRange(
                    new IdentityUserRole<string> { RoleId = ADMIN_ROLE_ID, UserId = ADMIN_USER_ID },
                    new IdentityUserRole<string> { RoleId = PROPOSER_ROLE_ID, UserId = PROPOSER_USER_ID },
                    new IdentityUserRole<string> { RoleId = USER_ROLE_ID, UserId = NORMAL_USER_ID }
                );

                await context.SaveChangesAsync();
            }
        }
    }
}