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
        [HttpPost]
        public async Task<IActionResult> TopUp(string steamId, string email, decimal amount, string paymentMethod)
        {
            if (string.IsNullOrEmpty(steamId) || string.IsNullOrEmpty(email) || amount <= 0)
            {
                TempData["Error"] = "Пожалуйста, заполните все поля корректно";
                return RedirectToAction("SteamTopUp", "Home");
            }

            try
            {
                // You can use the paymentMethod parameter here for further processing
                // For now, we'll just store it in a comment to show it's being passed
                // paymentMethod can be: "card", "qiwi", "webmoney", or "yoomoney"

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