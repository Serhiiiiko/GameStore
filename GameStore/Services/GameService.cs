using GameStore.Interfaces;
using GameStore.Models;

namespace GameStore.Services
{
    public class GameService : IGameService
    {
        private readonly IGameRepository _gameRepository;

        public GameService(IGameRepository gameRepository)
        {
            _gameRepository = gameRepository;
        }

        public async Task<IEnumerable<Game>> GetAllGamesAsync()
        {
            return await _gameRepository.GetAllAsync();
        }

        public async Task<Game?> GetGameByIdAsync(int id)
        {
            return await _gameRepository.GetByIdAsync(id);
        }

        public async Task<Game> CreateGameAsync(Game game)
        {
            return await _gameRepository.CreateAsync(game);
        }

        public async Task UpdateGameAsync(Game game)
        {
            await _gameRepository.UpdateAsync(game);
        }

        public async Task DeleteGameAsync(int id)
        {
            await _gameRepository.DeleteAsync(id);
        }
    }
}