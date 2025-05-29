using GameStore.Models;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(string email, int gameId, int? userId = null);
    Task<IEnumerable<Order>> GetOrdersByUserIdAsync(int userId);
    Task<string> GenerateGameKeyAsync();
    Task<bool> SendOrderConfirmationEmailAsync(Order order);
}