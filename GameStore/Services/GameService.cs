// Example of using memory cache in a service (modify GameService.cs)
using GameStore.Interfaces;
using GameStore.Models;
using Microsoft.Extensions.Caching.Memory;

public class GameService : IGameService
{
    private readonly IGameRepository _gameRepository;
    private readonly IMemoryCache _memoryCache;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(10);

    public GameService(IGameRepository gameRepository, IMemoryCache memoryCache)
    {
        _gameRepository = gameRepository;
        _memoryCache = memoryCache;
    }

    public async Task<IEnumerable<Game>> GetAllGamesAsync()
    {
        string cacheKey = "AllGames";
        if (!_memoryCache.TryGetValue(cacheKey, out IEnumerable<Game> games))
        {
            games = await _gameRepository.GetAllAsync();
            _memoryCache.Set(cacheKey, games, _cacheDuration);
        }
        return games;
    }

    public async Task<Game?> GetGameByIdAsync(int id)
    {
        string cacheKey = $"Game_{id}";
        if (!_memoryCache.TryGetValue(cacheKey, out Game? game))
        {
            game = await _gameRepository.GetByIdAsync(id);
            if (game != null)
            {
                _memoryCache.Set(cacheKey, game, _cacheDuration);
            }
        }
        return game;
    }

    public async Task<Game> CreateGameAsync(Game game)
    {
        var result = await _gameRepository.CreateAsync(game);
        _memoryCache.Remove("AllGames");
        return result;
    }

    public async Task UpdateGameAsync(Game game)
    {
        await _gameRepository.UpdateAsync(game);
        _memoryCache.Remove("AllGames");
        _memoryCache.Remove($"Game_{game.Id}");
    }

    public async Task DeleteGameAsync(int id)
    {
        await _gameRepository.DeleteAsync(id);
        _memoryCache.Remove("AllGames");
        _memoryCache.Remove($"Game_{id}");
    }
}