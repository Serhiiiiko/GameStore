using GameStore.Interfaces;
using GameStore.Models;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace GameStore.Services
{
    public class TelegramNotificationService : ITelegramNotificationService
    {
        private readonly TelegramBotClient _botClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TelegramNotificationService> _logger;
        private readonly List<string> _chatIds;

        public TelegramNotificationService(
            IConfiguration configuration,
            ILogger<TelegramNotificationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            string botToken = _configuration["TelegramBot:Token"] ?? throw new ArgumentNullException("TelegramBot:Token");

            // Get list of chat IDs separated by commas
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
                         $"Ключ: `{order.Key}`";

            await SendToAllChatsAsync(message, ParseMode.Markdown);
        }

        public async Task SendSteamTopUpNotificationAsync(SteamTopUp topUp)
        {
            var message = $"💰 Новое пополнение Steam!\n\n" +
                         $"ID операции: {topUp.Id}\n" +
                         $"Steam ID: {topUp.SteamId}\n" +
                         $"Сумма: {topUp.Amount}₽\n" +
                         $"Email: {topUp.Email}\n" +
                         $"Дата: {topUp.Date:dd.MM.yyyy HH:mm:ss}";

            await SendToAllChatsAsync(message);
        }

        public async Task SendHealthAlertAsync(string message)
        {
            // For health alerts, we want to ensure delivery
            var fullMessage = $"🚨 *SYSTEM HEALTH ALERT*\n\n{message}";
            await SendToAllChatsAsync(fullMessage, ParseMode.Markdown, priority: true);
        }

        public async Task SendServiceRestartNotificationAsync(string serviceName, string reason)
        {
            var message = $"🔄 *Service Restart*\n\n" +
                         $"Service: {serviceName}\n" +
                         $"Reason: {reason}\n" +
                         $"Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n" +
                         $"Environment: {_configuration["ASPNETCORE_ENVIRONMENT"]}";

            await SendToAllChatsAsync(message, ParseMode.Markdown);
        }

        public async Task SendStorageAlertAsync(string message)
        {
            var fullMessage = $"💾 *Storage System Alert*\n\n{message}";
            await SendToAllChatsAsync(fullMessage, ParseMode.Markdown);
        }

        private async Task SendToAllChatsAsync(string message, ParseMode parseMode = ParseMode.None, bool priority = false)
        {
            var tasks = _chatIds.Select(chatId => SendMessageSafelyAsync(chatId, message, parseMode, priority));
            await Task.WhenAll(tasks);
        }

        private async Task SendMessageSafelyAsync(string chatId, string message, ParseMode parseMode = ParseMode.None, bool priority = false)
        {
            int retryCount = priority ? 3 : 1;
            int retryDelay = 1000;

            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: message,
                        parseMode: parseMode,
                        disableNotification: false
                    );
                    return; // Success
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error sending notification to chatId {chatId}, attempt {i + 1} of {retryCount}");

                    if (i < retryCount - 1)
                    {
                        await Task.Delay(retryDelay);
                        retryDelay *= 2; // Exponential backoff
                    }
                }
            }
        }
    }
}