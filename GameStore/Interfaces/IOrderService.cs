using GameStore.Models;

namespace GameStore.Interfaces
{
    public interface IOrderService
    {
        Task<Order> CreateOrderAsync(string email, int gameId);
        Task<IEnumerable<Order>> GetOrdersByEmailAsync(string email);
        Task<string> GenerateGameKeyAsync();
        Task<bool> SendOrderConfirmationEmailAsync(Order order);
    }
}