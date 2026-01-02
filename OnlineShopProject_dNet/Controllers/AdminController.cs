using System;
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
        public async Task<IActionResult> Users(string? search, int page = 1, int pageSize = 10)
        {
            page = Math.Max(1, page);

            var usersQuery = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                usersQuery = usersQuery.Where(u =>
                    (u.Email ?? "").Contains(search) ||
                    (u.UserName ?? "").Contains(search) ||
                    (u.FirstName ?? "").Contains(search) ||
                    (u.LastName ?? "").Contains(search));
            }

            var users = usersQuery.ToList();
            var model = new List<AdminUserVm>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                model.Add(new AdminUserVm
                {
                    Id = user.Id,
                    Email = user.Email ?? user.UserName ?? "",
                    Name = string.Join(" ", new[] { user.FirstName, user.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim(),
                    IsProposer = roles.Contains("Proposer"),
                    IsAdmin = roles.Contains("Admin")
                });
            }

            // Daca nu exista cautare, afisam doar Proposerii
            if (string.IsNullOrWhiteSpace(search))
            {
                model = model.Where(m => m.IsProposer).ToList();
            }

            var totalCount = model.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var items = model.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var vm = new AdminUsersPageVm
            {
                Items = items,
                Page = page,
                TotalPages = totalPages,
                Search = search,
                TotalCount = totalCount
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleProposer(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Users");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["message"] = "Utilizatorul nu a fost gasit.";
                return RedirectToAction("Users");
            }

            var isProposer = await _userManager.IsInRoleAsync(user, "Proposer");

            IdentityResult result;
            string message;

            if (isProposer)
            {
                result = await _userManager.RemoveFromRoleAsync(user, "Proposer");
                message = "Rolul de Proposer ti-a fost revocat de administrator.";
            }
            else
            {
                if (!await _roleManager.RoleExistsAsync("Proposer"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Proposer"));
                }
                result = await _userManager.AddToRoleAsync(user, "Proposer");
                message = "Ai primit rolul de Proposer. Poti propune produse spre aprobare.";
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

    public class AdminUsersPageVm
    {
        public IEnumerable<AdminUserVm> Items { get; set; } = Enumerable.Empty<AdminUserVm>();
        public int Page { get; set; }
        public int TotalPages { get; set; }
        public string? Search { get; set; }
        public int TotalCount { get; set; }
    }
}
