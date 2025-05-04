using GameStore.Models;

namespace GameStore.Interfaces
{
    public interface ITelegramNotificationService
    {
        Task SendGamePurchaseNotificationAsync(Order order);
        Task SendSteamTopUpNotificationAsync(SteamTopUp topUp);
    }
}