using GameStore.Interfaces;
using GameStore.Models;

namespace GameStore.Services
{
    public class SteamTopUpService : ISteamTopUpService
    {
        private readonly ISteamTopUpRepository _steamTopUpRepository;
        private readonly IEmailService _emailService;
        private readonly ITelegramNotificationService _telegramService;

        public SteamTopUpService(
            ISteamTopUpRepository steamTopUpRepository,
            IEmailService emailService,
            ITelegramNotificationService telegramService)
        {
            _steamTopUpRepository = steamTopUpRepository;
            _emailService = emailService;
            _telegramService = telegramService;
        }

        public async Task<SteamTopUp> CreateTopUpAsync(string steamId, string email, decimal amount)
        {
            var topUp = new SteamTopUp
            {
                SteamId = steamId,
                Email = email,
                Amount = amount,
                Date = DateTime.UtcNow,
                IsCompleted = true
            };

            var createdTopUp = await _steamTopUpRepository.CreateAsync(topUp);

            // Send email confirmation
            await SendTopUpConfirmationEmailAsync(createdTopUp);

            // Send notification to Telegram
            await _telegramService.SendSteamTopUpNotificationAsync(createdTopUp);

            return createdTopUp;
        }

        public async Task<bool> SendTopUpConfirmationEmailAsync(SteamTopUp topUp)
        {
            try
            {
                await _emailService.SendSteamTopUpConfirmationAsync(topUp);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}