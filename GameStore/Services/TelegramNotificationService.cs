using GameStore.Interfaces;
using GameStore.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace GameStore.Services
{
    public class TelegramNotificationService : ITelegramNotificationService
    {
        private readonly TelegramBotClient _botClient;
        private readonly IConfiguration _configuration;
        private readonly string _chatId;

        public TelegramNotificationService(IConfiguration configuration)
        {
            _configuration = configuration;
            string botToken = _configuration["TelegramBot:Token"] ?? throw new ArgumentNullException("TelegramBot:Token");
            _chatId = _configuration["TelegramBot:ChatId"] ?? throw new ArgumentNullException("TelegramBot:ChatId");
            _botClient = new TelegramBotClient(botToken);
        }

        public async Task SendGamePurchaseNotificationAsync(Order order)
        {
            var message = $"🎮 Новая покупка игры!\n\n" +
                         $"ID заказа: {order.Id}\n" +
                         $"Игра: {order.Game?.Title}\n" +
                         $"Цена: {order.Game?.Price}₽\n" +
                         $"Email: {order.Email}\n" +
                         $"Дата: {order.OrderDate:dd.MM.yyyy HH:mm:ss}\n" +
                         $"Ключ: {order.Key}";

            await _botClient.SendMessage(chatId: _chatId, text: message);
        }

        public async Task SendSteamTopUpNotificationAsync(SteamTopUp topUp)
        {
            var message = $"💰 Новое пополнение Steam!\n\n" +
                         $"ID операции: {topUp.Id}\n" +
                         $"Steam ID: {topUp.SteamId}\n" +
                         $"Сумма: {topUp.Amount}₽\n" +
                         $"Email: {topUp.Email}\n" +
                         $"Дата: {topUp.Date:dd.MM.yyyy HH:mm:ss}";

            await _botClient.SendMessage(chatId: _chatId, text: message);
        }
    }
}