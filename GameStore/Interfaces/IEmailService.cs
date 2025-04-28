using GameStore.Models;

namespace GameStore.Interfaces
{
    public interface IEmailService
    {
        Task SendOrderConfirmationAsync(Order order);
        Task SendSteamTopUpConfirmationAsync(SteamTopUp topUp);
    }
}