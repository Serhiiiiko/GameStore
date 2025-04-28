using GameStore.Models;

namespace GameStore.Interfaces
{
    public interface IOrderRepository
    {
        Task<IEnumerable<Order>> GetAllAsync();
        Task<Order?> GetByIdAsync(int id);
        Task<IEnumerable<Order>> GetByEmailAsync(string email);
        Task<Order> CreateAsync(Order order);
        Task UpdateAsync(Order order);
    }
}