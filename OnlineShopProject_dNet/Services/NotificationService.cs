using System;
using OnlineShopProject_dNet.Data;
using OnlineShopProject_dNet.Models;
using Microsoft.EntityFrameworkCore;

namespace OnlineShopProject_dNet.Services
{
    public class NotificationService(ApplicationDbContext context)
    {
        private readonly ApplicationDbContext _context = context;

        public async Task AddNotificationAsync(string userId, string message, string? type = null)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(message)) return;

            var notif = new Notification
            {
                UserId = userId,
                Message = message,
                Type = type,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notif);
            await _context.SaveChangesAsync();
        }

        public async Task<int> UnreadCountAsync(string userId)
        {
            return await _context.Notifications.Where(n => n.UserId == userId && !n.IsRead).CountAsync();
        }

        public async Task MarkAsReadAsync(string userId, int id)
        {
            var notif = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
            if (notif != null && !notif.IsRead)
            {
                notif.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var notifs = await _context.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
            if (notifs.Count == 0) return;
            foreach (var n in notifs) n.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }
}
