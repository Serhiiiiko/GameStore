using GameStore.Interfaces;
using GameStore.Models;

namespace GameStore.Services
{
    public class SteamTopUpService : ISteamTopUpService
    {
        private readonly ISteamTopUpRepository _steamTopUpRepository;
        private readonly IEmailService _emailService;

        public SteamTopUpService(
            ISteamTopUpRepository steamTopUpRepository,
            IEmailService emailService)
        {
            _steamTopUpRepository = steamTopUpRepository;
            _emailService = emailService;
        }

        public async Task<SteamTopUp> CreateTopUpAsync(string steamId, string email, decimal amount)
        {
            var topUp = new SteamTopUp
            {
                SteamId = steamId,
                Email = email,
                Amount = amount,
                Date = DateTime.Now,
                IsCompleted = true
            };

            var createdTopUp = await _steamTopUpRepository.CreateAsync(topUp);
            await SendTopUpConfirmationEmailAsync(createdTopUp);

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
