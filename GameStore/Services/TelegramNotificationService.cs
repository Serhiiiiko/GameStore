using GameStore.Interfaces;
using GameStore.Models;
using Telegram.Bot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameStore.Services
{
    public class TelegramNotificationService : ITelegramNotificationService
    {
        private readonly TelegramBotClient _botClient;
        private readonly IConfiguration _configuration;
        private readonly List<string> _chatIds;

        public TelegramNotificationService(IConfiguration configuration)
        {
            _configuration = configuration;
            string botToken = _configuration["TelegramBot:Token"] ?? throw new ArgumentNullException("TelegramBot:Token");

            // Получаем список ID чатов, разделенных запятыми
            string chatIdsConfig = _configuration["TelegramBot:ChatIds"] ?? _configuration["TelegramBot:ChatId"] ?? throw new ArgumentNullException("TelegramBot:ChatIds");
            _chatIds = chatIdsConfig.Split(',').Select(id => id.Trim()).ToList();

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

            // Отправляем всем получателям параллельно
            await Task.WhenAll(_chatIds.Select(chatId =>
                SendMessageSafelyAsync(chatId, message)));
        }

        public async Task SendSteamTopUpNotificationAsync(SteamTopUp topUp)
        {
            var message = $"💰 Новое пополнение Steam!\n\n" +
                         $"ID операции: {topUp.Id}\n" +
                         $"Steam ID: {topUp.SteamId}\n" +
                         $"Сумма: {topUp.Amount}₽\n" +
                         $"Email: {topUp.Email}\n" +
                         $"Дата: {topUp.Date:dd.MM.yyyy HH:mm:ss}";

            // Отправляем всем получателям параллельно
            await Task.WhenAll(_chatIds.Select(chatId =>
                SendMessageSafelyAsync(chatId, message)));
        }

        // Вспомогательный метод для безопасной отправки сообщений
        private async Task SendMessageSafelyAsync(string chatId, string message)
        {
            try
            {
                await _botClient.SendMessage(chatId: chatId, text: message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки уведомления для chatId {chatId}: {ex.Message}");
                // В реальном приложении здесь должно быть логирование через ILogger
            }
        }
    }
}