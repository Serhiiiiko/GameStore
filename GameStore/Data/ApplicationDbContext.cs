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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed initial admin user with hashed password 'admin123'
            modelBuilder.Entity<AdminUser>().HasData(
                new AdminUser
                {
                    Id = 1,
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin654123"),
                    Role = "Admin"
                }
            );

            // Seed initial games data
            modelBuilder.Entity<Game>().HasData(
                new Game
                {
                    Id = 1,
                    Title = "Fortnite",
                    Description = "Battle royale game with building mechanics",
                    Genre = "Песочница",
                    Price = 1200,
                    ImageUrl = "/images/popular-01.jpg",
                    Rating = 4.8,
                    Downloads = "2.3M",
                    Platform = PlatformType.Steam
                },
                new Game
                {
                    Id = 2,
                    Title = "PubG",
                    Description = "Realistic battle royale shooter",
                    Genre = "Королевская битва",
                    Price = 1500,
                    ImageUrl = "/images/popular-02.jpg",
                    Rating = 4.8,
                    Downloads = "2.3M",
                    Platform = PlatformType.Origin
                },
                // Add more games here
                new Game
                {
                    Id = 3,
                    Title = "Dota2",
                    Description = "Competitive MOBA game",
                    Genre = "Steam-X",
                    Price = 200,
                    ImageUrl = "/images/popular-03.jpg",
                    Rating = 4.8,
                    Downloads = "2.3M",
                    Platform = PlatformType.Xbox
                }
            );
        }
    }
}