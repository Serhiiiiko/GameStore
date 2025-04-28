using GameStore.Models;

namespace GameStore.Interfaces
{
    public interface ISteamTopUpService
    {
        Task<SteamTopUp> CreateTopUpAsync(string steamId, string email, decimal amount);
        Task<bool> SendTopUpConfirmationEmailAsync(SteamTopUp topUp);
    }
}