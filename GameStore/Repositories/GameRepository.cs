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

        public GameRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Game>> GetAllAsync()
        {
            try
            {
                // Попытка получить все игры с учетом свойства Platform
                return await _context.Games.ToListAsync();
            }
            catch (PostgresException ex)
            {
                // Если ошибка связана с отсутствием столбца Platform
                if (ex.SqlState == "42703" && ex.Message.Contains("Platform"))
                {
                    // Выполняем SQL-запрос для добавления колонки
                    await _context.Database.ExecuteSqlRawAsync(
                        "ALTER TABLE \"Games\" ADD COLUMN IF NOT EXISTS \"Platform\" text NOT NULL DEFAULT 'Steam'");

                    // Повторяем запрос после добавления колонки
                    return await _context.Games.ToListAsync();
                }

                // Если это другая ошибка, пробрасываем её дальше
                throw;
            }
        }
        public async Task<Game?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Games.FindAsync(id);
            }
            catch (PostgresException ex)
            {
                if (ex.SqlState == "42703" && ex.Message.Contains("Platform"))
                {
                    await _context.Database.ExecuteSqlRawAsync(
                        "ALTER TABLE \"Games\" ADD COLUMN IF NOT EXISTS \"Platform\" text NOT NULL DEFAULT 'Steam'");

                    return await _context.Games.FindAsync(id);
                }
                throw;
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
            // Check if entity is already being tracked
            var existingEntity = await _context.Games.FindAsync(game.Id);

            if (existingEntity != null)
            {
                // Update the properties of the tracked entity
                _context.Entry(existingEntity).CurrentValues.SetValues(game);
            }
            else
            {
                // If no entity is being tracked, attach and mark as modified
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