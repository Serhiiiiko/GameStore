using Microsoft.EntityFrameworkCore;
using GameStore.Models;

namespace GameStore.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Game> Games { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<SteamTopUp> SteamTopUps { get; set; }
        public DbSet<AdminUser> AdminUsers { get; set; }
        public DbSet<User> Users { get; set; } // Add Users table
        public DbSet<PaymentProvider> PaymentProviders { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Game>()
                .Property(g => g.Platform)
                .HasConversion<string>()
                .HasDefaultValue(PlatformType.Steam);

            // Configure User relationships
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SteamTopUp>()
                .HasOne(s => s.User)
                .WithMany(u => u.SteamTopUps)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PaymentProvider>()
     .HasIndex(p => p.Name)
     .IsUnique();

            modelBuilder.Entity<PaymentTransaction>()
                .HasIndex(t => t.TransactionId)
                .IsUnique();

            modelBuilder.Entity<PaymentTransaction>()
                .HasOne(t => t.Provider)
                .WithMany(p => p.Transactions)
                .HasForeignKey(t => t.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed тестового провайдера
            modelBuilder.Entity<PaymentProvider>().HasData(
                new PaymentProvider
                {
                    Id = 1,
                    Name = "test",
                    DisplayName = "Тестовый провайдер",
                    Description = "Провайдер для тестирования платежей в режиме разработки",
                    Icon = "fas fa-vial",
                    IsActive = true,
                    IsDefault = true,
                    Configuration = "{\"test_mode\":true,\"auto_complete\":true}",
                    CreatedAt = DateTime.UtcNow
                });
            

                        modelBuilder.Entity<AdminUser>().HasData(
                new AdminUser
                {
                    Id = 1,
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin654123"),
                    Role = "Admin"
                }
            );

        }
    }
}