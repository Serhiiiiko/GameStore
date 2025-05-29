using GameStore.Interfaces;
using GameStore.Models;

namespace GameStore.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IGameRepository _gameRepository;
        private readonly IEmailService _emailService;
        private readonly ITelegramNotificationService _telegramService;

        public OrderService(
            IOrderRepository orderRepository,
            IGameRepository gameRepository,
            IEmailService emailService,
            ITelegramNotificationService telegramService)
        {
            _orderRepository = orderRepository;
            _gameRepository = gameRepository;
            _emailService = emailService;
            _telegramService = telegramService;
        }

        public async Task<Order> CreateOrderAsync(string email, int gameId, int? userId = null)
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
                UserId = userId,
                Key = await GenerateGameKeyAsync(),
                OrderDate = DateTime.UtcNow,
                IsCompleted = true
            };

            var createdOrder = await _orderRepository.CreateAsync(order);

            // Load the associated Game for the notification
            createdOrder.Game = game;

            // Send email confirmation
            await SendOrderConfirmationEmailAsync(createdOrder);

            // Send notification to Telegram
            await _telegramService.SendGamePurchaseNotificationAsync(createdOrder);

            return createdOrder;
        }

        public async Task<IEnumerable<Order>> GetOrdersByUserIdAsync(int userId)
        {
            return await _orderRepository.GetByUserIdAsync(userId);
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