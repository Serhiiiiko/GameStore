using GameStore.Interfaces;
using GameStore.Models;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers
{
    public class SteamController : Controller
    {
        private readonly ISteamTopUpService _steamTopUpService;

        public SteamController(ISteamTopUpService steamTopUpService)
        {
            _steamTopUpService = steamTopUpService;
        }

        [HttpPost]
        public async Task<IActionResult> TopUp(string steamId, string email, decimal amount)
        {
            if (string.IsNullOrEmpty(steamId) || string.IsNullOrEmpty(email) || amount <= 0)
            {
                TempData["Error"] = "Пожалуйста, заполните все поля корректно";
                return RedirectToAction("SteamTopUp", "Home");
            }

            try
            {
                await _steamTopUpService.CreateTopUpAsync(steamId, email, amount);
                TempData["Success"] = "Пополнение успешно выполнено. Проверьте вашу почту для получения подтверждения.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка при пополнении: {ex.Message}";
            }

            return RedirectToAction("SteamTopUp", "Home");
        }
    }
}