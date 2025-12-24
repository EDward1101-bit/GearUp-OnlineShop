using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OnlineShopProject_dNet.Models;

namespace OnlineShopProject_dNet.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Este OBLIGATORIU să apelăm metoda de bază pentru ca Identity (Useri, Roluri) să funcționeze
            base.OnModelCreating(modelBuilder);

            // DEFINIRE RELATII SI CASCADE DELETE

            // 1. Cand stergem un Produs, se sterg automat toate Review-urile asociate
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // 2. Cand stergem un User, se sterg automat Review-urile lui
            // (Sau putem pune Restrict daca vrem sa pastram review-urile dar sa facem User null, 
            // dar Cascade e mai curat pentru inceput)
            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}