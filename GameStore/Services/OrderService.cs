using GameStore.Interfaces;
using GameStore.Models;

namespace GameStore.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IGameRepository _gameRepository;
        private readonly IEmailService _emailService;

        public OrderService(
            IOrderRepository orderRepository,
            IGameRepository gameRepository,
            IEmailService emailService)
        {
            _orderRepository = orderRepository;
            _gameRepository = gameRepository;
            _emailService = emailService;
        }

        public async Task<Order> CreateOrderAsync(string email, int gameId)
        {
            var game = await _gameRepository.GetByIdAsync(gameId);
            if (game == null)
            {
                throw new Exception($"Game with ID {gameId} not found.");
            }

            var order = new Order
            {
                Email = email,
                GameId = gameId,
                Key = await GenerateGameKeyAsync(),
                OrderDate = DateTime.Now,
                IsCompleted = true
            };

            var createdOrder = await _orderRepository.CreateAsync(order);
            await SendOrderConfirmationEmailAsync(createdOrder);

            return createdOrder;
        }

        public async Task<IEnumerable<Order>> GetOrdersByEmailAsync(string email)
        {
            return await _orderRepository.GetByEmailAsync(email);
        }

        public async Task<string> GenerateGameKeyAsync()
        {
            return Guid.NewGuid().ToString().ToUpper();
        }

        public async Task<bool> SendOrderConfirmationEmailAsync(Order order)
        {
            try
            {
                await _emailService.SendOrderConfirmationAsync(order);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}