using GameStore.Models;

namespace GameStore.Interfaces
{
    public interface ISteamTopUpService
    {
        Task<SteamTopUp> CreateTopUpAsync(string steamId, string email, decimal amount, int? userId = null);
        Task<bool> SendTopUpConfirmationEmailAsync(SteamTopUp topUp);
    }
}