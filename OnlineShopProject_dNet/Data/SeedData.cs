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
                // Așteptăm aplicarea migrațiilor pentru a fi siguri că DB-ul există
                // context.Database.Migrate(); 

                var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                // --- CREAREA ROLURILOR ---
                // Verificăm și creăm rolurile dacă nu există
                string[] roleNames = { "Admin", "Proposer", "User" };
                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }

                // --- CREAREA UTILIZATORILOR ---
                // Folosim UserManager pentru că face automat Hash la parolă și validează datele
               

                // ID-uri fixe
                const string ADMIN_ID = "9e445865-a24d-4543-a6c6-9443d048cdb9";
                const string PROPOSER_ID = "9e445865-a24d-4543-a6c6-9443d048cdb8";
                const string USER_ID = "9e445865-a24d-4543-a6c6-9443d048cdb7";

                // User ADMIN
                if (await userManager.FindByIdAsync(ADMIN_ID) == null)
                {
                    var adminUser = new ApplicationUser
                    {
                        Id = ADMIN_ID,
                        UserName = "admin@test.com",
                        Email = "admin@test.com",
                        EmailConfirmed = true,
                        FirstName = "Admin",
                        LastName = "Sistem"
                    };

                    // CreateAsync face automat Hash la parolă
                    var result = await userManager.CreateAsync(adminUser, "Admin123!");

                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                    }
                }

                // User PROPOSER (Colaborator)
                if (await userManager.FindByIdAsync(PROPOSER_ID) == null)
                {
                    var proposerUser = new ApplicationUser
                    {
                        Id = PROPOSER_ID,
                        UserName = "proposer@test.com",
                        Email = "proposer@test.com",
                        EmailConfirmed = true,
                        FirstName = "Dan",
                        LastName = "Propunator"
                    };

                    var result = await userManager.CreateAsync(proposerUser, "Proposer123!");

                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(proposerUser, "Proposer");
                    }
                }

                // User NORMAL (Vizitator Inregistrat)
                if (await userManager.FindByIdAsync(USER_ID) == null)
                {
                    var normalUser = new ApplicationUser
                    {
                        Id = USER_ID,
                        UserName = "user@test.com",
                        Email = "user@test.com",
                        EmailConfirmed = true,
                        FirstName = "Ion",
                        LastName = "Userescu"
                    };

                    var result = await userManager.CreateAsync(normalUser, "User123!");

                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(normalUser, "User");
                    }
                }
            }
        }
    }
}