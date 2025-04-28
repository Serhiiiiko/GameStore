using GameStore.Models;

namespace GameStore.Interfaces
{
    public interface ISteamTopUpRepository
    {
        Task<IEnumerable<SteamTopUp>> GetAllAsync();
        Task<SteamTopUp?> GetByIdAsync(int id);
        Task<IEnumerable<SteamTopUp>> GetBySteamIdAsync(string steamId);
        Task<SteamTopUp> CreateAsync(SteamTopUp topUp);
        Task UpdateAsync(SteamTopUp topUp);
    }
}