using GameStore.Models;

namespace GameStore.Interfaces
{
    public interface IGameRepository
    {
        Task<IEnumerable<Game>> GetAllAsync();
        Task<Game?> GetByIdAsync(int id);
        Task<Game> CreateAsync(Game game);
        Task UpdateAsync(Game game);
        Task DeleteAsync(int id);
    }
}