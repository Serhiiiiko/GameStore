using GameStore.Data;
using GameStore.Interfaces;
using GameStore.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GameStore.Repositories
{
    public class GameRepository : IGameRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GameRepository> _logger;

        public GameRepository(ApplicationDbContext context, ILogger<GameRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Game>> GetAllAsync()
        {
            try
            {
                return await _context.Games.ToListAsync();
            }
            catch (InvalidCastException ex)
            {
                _logger.LogWarning("Platform type mismatch detected. Attempting to fix...");

                // Получаем данные напрямую через SQL
                var games = new List<Game>();

                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = @"
                        SELECT ""Id"", ""Title"", ""Description"", ""Genre"", ""Price"", 
                               ""ImageUrl"", ""Rating"", ""Downloads"", ""Platform""
                        FROM ""Games""";

                    await _context.Database.OpenConnectionAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var game = new Game
                            {
                                Id = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Description = reader.GetString(2),
                                Genre = reader.GetString(3),
                                Price = reader.GetDecimal(4),
                                ImageUrl = reader.GetString(5),
                                Rating = reader.GetDouble(6),
                                Downloads = reader.GetString(7)
                            };

                            // Обработка Platform
                            if (reader.IsDBNull(8))
                            {
                                game.Platform = PlatformType.Steam;
                            }
                            else
                            {
                                var platformValue = reader.GetValue(8);

                                if (platformValue is int intValue)
                                {
                                    // Конвертируем int в enum
                                    game.Platform = (PlatformType)intValue;
                                }
                                else if (platformValue is string stringValue)
                                {
                                    // Парсим string в enum
                                    Enum.TryParse<PlatformType>(stringValue, out var platform);
                                    game.Platform = platform;
                                }
                                else
                                {
                                    game.Platform = PlatformType.Steam;
                                }
                            }

                            games.Add(game);
                        }
                    }

                    await _context.Database.CloseConnectionAsync();
                }

                // Автоматическое исправление типа в базе данных
                await FixPlatformTypeAsync();

                return games;
            }
        }

        private async Task FixPlatformTypeAsync()
        {
            try
            {
                _logger.LogInformation("Attempting to fix Platform column type...");

                await _context.Database.ExecuteSqlRawAsync(@"
                    DO $$
                    BEGIN
                        -- Проверяем тип колонки Platform
                        IF EXISTS (
                            SELECT 1 
                            FROM information_schema.columns 
                            WHERE table_name = 'Games' 
                            AND column_name = 'Platform' 
                            AND data_type = 'integer'
                        ) THEN
                            -- Создаем временную колонку
                            ALTER TABLE ""Games"" ADD COLUMN IF NOT EXISTS ""PlatformTemp"" text;
                            
                            -- Конвертируем значения
                            UPDATE ""Games"" 
                            SET ""PlatformTemp"" = 
                                CASE 
                                    WHEN ""Platform"" = 0 THEN 'Steam'
                                    WHEN ""Platform"" = 1 THEN 'Origin'
                                    WHEN ""Platform"" = 2 THEN 'Xbox'
                                    ELSE 'Steam'
                                END;
                            
                            -- Удаляем старую колонку
                            ALTER TABLE ""Games"" DROP COLUMN ""Platform"";
                            
                            -- Переименовываем временную колонку
                            ALTER TABLE ""Games"" RENAME COLUMN ""PlatformTemp"" TO ""Platform"";
                            
                            -- Устанавливаем NOT NULL и значение по умолчанию
                            ALTER TABLE ""Games"" ALTER COLUMN ""Platform"" SET NOT NULL;
                            ALTER TABLE ""Games"" ALTER COLUMN ""Platform"" SET DEFAULT 'Steam';
                        END IF;
                    END $$;
                ");

                _logger.LogInformation("Platform column type fixed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fix Platform column type");
            }
        }

        public async Task<Game?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Games.FindAsync(id);
            }
            catch (InvalidCastException)
            {
                // Если есть проблема с типом, сначала исправляем
                await FixPlatformTypeAsync();
                return await _context.Games.FindAsync(id);
            }
        }

        public async Task<Game> CreateAsync(Game game)
        {
            _context.Games.Add(game);
            await _context.SaveChangesAsync();
            return game;
        }

        public async Task UpdateAsync(Game game)
        {
            var existingEntity = await _context.Games.FindAsync(game.Id);

            if (existingEntity != null)
            {
                _context.Entry(existingEntity).CurrentValues.SetValues(game);
            }
            else
            {
                _context.Entry(game).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var game = await _context.Games.FindAsync(id);
            if (game != null)
            {
                _context.Games.Remove(game);
                await _context.SaveChangesAsync();
            }
        }
    }
}