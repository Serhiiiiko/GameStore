using GameStore.Models;

namespace GameStore.Interfaces
{
    public interface ITelegramNotificationService
    {
        Task SendGamePurchaseNotificationAsync(Order order);
        Task SendSteamTopUpNotificationAsync(SteamTopUp topUp);
        Task SendHealthAlertAsync(string message);
        Task SendServiceRestartNotificationAsync(string serviceName, string reason);
        Task SendStorageAlertAsync(string message);
    }
}