using GameStore.Data;
using GameStore.Interfaces;
using GameStore.Models;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDbContext _context;

        public OrderRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Order>> GetAllAsync()
        {
            return await _context.Orders.Include(o => o.Game).ToListAsync();
        }

        public async Task<Order?> GetByIdAsync(int id)
        {
            return await _context.Orders.Include(o => o.Game).FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<IEnumerable<Order>> GetByEmailAsync(string email)
        {
            return await _context.Orders
                .Include(o => o.Game)
                .Where(o => o.Email == email)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetByUserIdAsync(int userId)
        {
            return await _context.Orders
                .Include(o => o.Game)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }
        public async Task<Order> CreateAsync(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task UpdateAsync(Order order)
        {
            var existingOrder = await _context.Orders.FindAsync(order.Id);
            if (existingOrder != null)
            {
                existingOrder.Email = order.Email;
                existingOrder.GameId = order.GameId;
                existingOrder.UserId = order.UserId;
                existingOrder.Key = order.Key;
                existingOrder.OrderDate = order.OrderDate;
                existingOrder.IsCompleted = order.IsCompleted;

                _context.Entry(existingOrder).State = EntityState.Modified;
            }
            else
            {
                _context.Entry(order).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();
        }
    }
}