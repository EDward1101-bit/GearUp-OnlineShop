using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShopProject_dNet.Data;
using OnlineShopProject_dNet.Models;
using OnlineShopProject_dNet.Services;

namespace OnlineShopProject_dNet.Controllers
{
    [Authorize]
    public class NotificationsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, NotificationService notificationService) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly NotificationService _notificationService = notificationService;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            foreach (var n in notifications)
            {
                n.Message = NotificationService.SanitizeMessage(n.Message);
            }
            return View(notifications);
        }

        [HttpPost]
        public async Task<IActionResult> MarkRead(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId != null)
            {
                await _notificationService.MarkAsReadAsync(userId, id);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> MarkAll()
        {
            var userId = _userManager.GetUserId(User);
            if (userId != null)
            {
                await _notificationService.MarkAllAsReadAsync(userId);
            }
            return RedirectToAction("Index");
        }
    }
}
