using GameStore.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers
{
    public class GamesController : Controller
    {
        private readonly IGameService _gameService;
        private readonly IAuthService _authService;

        public GamesController(IGameService gameService, IAuthService authService)
        {
            _gameService = gameService;
            _authService = authService;
        }

        public async Task<IActionResult> Index()
        {
            var games = await _gameService.GetAllGamesAsync();
            ViewBag.IsAuthenticated = _authService.IsAuthenticated(HttpContext);
            return View(games);
        }

        public async Task<IActionResult> Details(int id)
        {
            var game = await _gameService.GetGameByIdAsync(id);
            if (game == null)
            {
                return NotFound();
            }
            ViewBag.IsAuthenticated = _authService.IsAuthenticated(HttpContext);
            return View(game);
        }

        [HttpPost]
        public async Task<IActionResult> Purchase(int gameId, string email)
        {
            // Check if user is authenticated
            if (!_authService.IsAuthenticated(HttpContext))
            {
                TempData["Error"] = "Пожалуйста, войдите в свой аккаунт для совершения покупки";
                return Json(new { requireLogin = true, returnUrl = Url.Action("Login", "Account", new { returnUrl = Url.Action("Index", "Games") }) });
            }

            var userId = _authService.GetCurrentUserId(HttpContext);
            if (userId == null)
            {
                return Json(new { success = false, message = "Ошибка аутентификации" });
            }

            try
            {
                var orderService = HttpContext.RequestServices.GetService<IOrderService>();
                if (orderService == null)
                {
                    return Json(new { success = false, message = "Сервис заказа недоступен." });
                }

                // Get current user's email
                var user = await _authService.GetCurrentUserAsync(HttpContext);
                if (user == null)
                {
                    return Json(new { success = false, message = "Пользователь не найден" });
                }

                var order = await orderService.CreateOrderAsync(user.Email, gameId, userId.Value);

                TempData["Success"] = "Покупка успешно завершена. Проверьте вашу почту для получения ключа.";
                return Json(new { success = true, redirectUrl = Url.Action("Orders", "Account") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Ошибка при покупке: {ex.Message}" });
            }
        }
    }
}