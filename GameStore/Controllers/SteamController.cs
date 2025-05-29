using GameStore.Interfaces;
using GameStore.Models;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers
{
    public class SteamController : Controller
    {
        private readonly ISteamTopUpService _steamTopUpService;
        private readonly IAuthService _authService;

        public SteamController(ISteamTopUpService steamTopUpService, IAuthService authService)
        {
            _steamTopUpService = steamTopUpService;
            _authService = authService;
        }

        [HttpPost]
        public async Task<IActionResult> TopUp(string steamId, string email, decimal amount, string paymentMethod)
        {
            // Check if user is authenticated
            if (!_authService.IsAuthenticated(HttpContext))
            {
                TempData["Error"] = "Пожалуйста, войдите в свой аккаунт для пополнения Steam";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("SteamTopUp", "Home") });
            }

            var userId = _authService.GetCurrentUserId(HttpContext);
            if (userId == null)
            {
                TempData["Error"] = "Ошибка аутентификации";
                return RedirectToAction("SteamTopUp", "Home");
            }

            if (string.IsNullOrEmpty(steamId) || amount <= 0)
            {
                TempData["Error"] = "Пожалуйста, заполните все поля корректно";
                return RedirectToAction("SteamTopUp", "Home");
            }

            try
            {
                // Get current user's email
                var user = await _authService.GetCurrentUserAsync(HttpContext);
                if (user == null)
                {
                    TempData["Error"] = "Пользователь не найден";
                    return RedirectToAction("SteamTopUp", "Home");
                }

                await _steamTopUpService.CreateTopUpAsync(steamId, user.Email, amount, userId.Value);
                TempData["Success"] = "Пополнение успешно выполнено. Проверьте вашу почту для получения подтверждения.";

                return RedirectToAction("TopUps", "Account");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка при пополнении: {ex.Message}";
            }

            return RedirectToAction("SteamTopUp", "Home");
        }
    }
}