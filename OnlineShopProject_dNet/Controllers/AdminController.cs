using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineShopProject_dNet.Models;
using OnlineShopProject_dNet.Services;

namespace OnlineShopProject_dNet.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, NotificationService notificationService) : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        private readonly NotificationService _notificationService = notificationService;

        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var users = _userManager.Users.ToList();
            var model = new List<AdminUserVm>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                model.Add(new AdminUserVm
                {
                    Id = user.Id,
                    Email = user.Email ?? user.UserName ?? "",
                    Name = string.Join(" ", new[] { user.FirstName, user.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)) ).Trim(),
                    IsProposer = roles.Contains("Proposer"),
                    IsAdmin = roles.Contains("Admin")
                });
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleProposer(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Users");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["message"] = "Utilizatorul nu a fost g?sit.";
                return RedirectToAction("Users");
            }

            var isProposer = await _userManager.IsInRoleAsync(user, "Proposer");

            IdentityResult result;
            string message;

            if (isProposer)
            {
                result = await _userManager.RemoveFromRoleAsync(user, "Proposer");
                message = "Rolul de Proposer ?i-a fost revocat de administrator.";
            }
            else
            {
                if (!await _roleManager.RoleExistsAsync("Proposer"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Proposer"));
                }
                result = await _userManager.AddToRoleAsync(user, "Proposer");
                message = "Ai primit rolul de Proposer. Po?i propune produse spre aprobare.";
            }

            if (result.Succeeded)
            {
                TempData["message"] = "Rol actualizat.";
                await _notificationService.AddNotificationAsync(user.Id, message, "role");
            }
            else
            {
                TempData["message"] = string.Join("; ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction("Users");
        }
    }

    public class AdminUserVm
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsProposer { get; set; }
        public bool IsAdmin { get; set; }
    }
}
