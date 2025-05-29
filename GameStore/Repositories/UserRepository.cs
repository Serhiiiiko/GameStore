using GameStore.Data;
using GameStore.Interfaces;
using GameStore.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GameStore.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(ApplicationDbContext context, ILogger<UserRepository> logger)
        {
            _context = context;
            _logger = logger;
            EnsureUsersTableExists();
        }

        private void EnsureUsersTableExists()
        {
            try
            {
                var connection = _context.Database.GetDbConnection();
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    // Check if Users table exists
                    command.CommandText = "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'Users')";
                    var exists = (bool)command.ExecuteScalar();

                    if (!exists)
                    {
                        _logger.LogWarning("Users table not found. Creating it now...");

                        command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS ""Users"" (
                                ""Id"" SERIAL PRIMARY KEY,
                                ""Email"" TEXT NOT NULL,
                                ""PasswordHash"" TEXT NOT NULL,
                                ""Name"" TEXT NOT NULL,
                                ""RegisteredAt"" TIMESTAMP WITH TIME ZONE NOT NULL,
                                ""LastLoginAt"" TIMESTAMP WITH TIME ZONE,
                                ""IsActive"" BOOLEAN NOT NULL
                            );
                            
                            ALTER TABLE ""Orders"" ADD COLUMN IF NOT EXISTS ""UserId"" INTEGER;
                            ALTER TABLE ""SteamTopUps"" ADD COLUMN IF NOT EXISTS ""UserId"" INTEGER;
                            
                            DO $$
                            BEGIN
                                IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Orders_Users_UserId') THEN
                                    ALTER TABLE ""Orders"" 
                                    ADD CONSTRAINT ""FK_Orders_Users_UserId"" 
                                    FOREIGN KEY (""UserId"") REFERENCES ""Users""(""Id"") 
                                    ON DELETE SET NULL;
                                END IF;
                                
                                IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_SteamTopUps_Users_UserId') THEN
                                    ALTER TABLE ""SteamTopUps"" 
                                    ADD CONSTRAINT ""FK_SteamTopUps_Users_UserId"" 
                                    FOREIGN KEY (""UserId"") REFERENCES ""Users""(""Id"") 
                                    ON DELETE SET NULL;
                                END IF;
                            END $$;
                            
                            INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"") 
                            VALUES ('20250529121202_User', '8.0.15')
                            ON CONFLICT (""MigrationId"") DO NOTHING;
                        ";

                        command.ExecuteNonQuery();
                        _logger.LogInformation("Users table created successfully.");
                    }
                }

                connection.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring Users table exists");
            }
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.Orders)
                        .ThenInclude(o => o.Game)
                    .Include(u => u.SteamTopUps)
                    .FirstOrDefaultAsync(u => u.Id == id);
            }
            catch (PostgresException ex) when (ex.SqlState == "42P01") // Table does not exist
            {
                _logger.LogError(ex, "Users table does not exist when trying to get user by ID");
                EnsureUsersTableExists();
                return null;
            }
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.Orders)
                        .ThenInclude(o => o.Game)
                    .Include(u => u.SteamTopUps)
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            }
            catch (PostgresException ex) when (ex.SqlState == "42P01") // Table does not exist
            {
                _logger.LogError(ex, "Users table does not exist when trying to get user by email");
                EnsureUsersTableExists();
                return null;
            }
        }

        public async Task<User> CreateAsync(User user)
        {
            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return user;
            }
            catch (PostgresException ex) when (ex.SqlState == "42P01") // Table does not exist
            {
                _logger.LogError(ex, "Users table does not exist when trying to create user");
                EnsureUsersTableExists();

                // Retry once after creating table
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return user;
            }
        }

        public async Task UpdateAsync(User user)
        {
            try
            {
                _context.Entry(user).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (PostgresException ex) when (ex.SqlState == "42P01") // Table does not exist
            {
                _logger.LogError(ex, "Users table does not exist when trying to update user");
                EnsureUsersTableExists();
                throw;
            }
        }

        public async Task<bool> ExistsAsync(string email)
        {
            try
            {
                return await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
            }
            catch (PostgresException ex) when (ex.SqlState == "42P01") // Table does not exist
            {
                _logger.LogError(ex, "Users table does not exist when checking if user exists");
                EnsureUsersTableExists();
                return false;
            }
        }
    }
}