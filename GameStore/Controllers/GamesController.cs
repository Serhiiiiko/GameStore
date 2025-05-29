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

        // Замените метод Purchase в файле GameStore/Controllers/GamesController.cs на этот:

        [HttpPost]
        public async Task<IActionResult> Purchase(int gameId, string email)
        {
            // Check if user is authenticated
            if (!_authService.IsAuthenticated(HttpContext))
            {
                TempData["Error"] = "Пожалуйста, войдите в свой аккаунт для совершения покупки";

                // Если это AJAX запрос, возвращаем JSON
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { requireLogin = true, returnUrl = Url.Action("Login", "Account", new { returnUrl = Url.Action("Index", "Games") }) });
                }

                // Иначе делаем обычный редирект
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Games") });
            }

            var userId = _authService.GetCurrentUserId(HttpContext);
            if (userId == null)
            {
                TempData["Error"] = "Ошибка аутентификации";

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Ошибка аутентификации" });
                }

                return RedirectToAction("Index", "Games");
            }

            try
            {
                var orderService = HttpContext.RequestServices.GetService<IOrderService>();
                if (orderService == null)
                {
                    TempData["Error"] = "Сервис заказа недоступен.";

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = "Сервис заказа недоступен." });
                    }

                    return RedirectToAction("Index", "Games");
                }

                // Get current user's email
                var user = await _authService.GetCurrentUserAsync(HttpContext);
                if (user == null)
                {
                    TempData["Error"] = "Пользователь не найден";

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = "Пользователь не найден" });
                    }

                    return RedirectToAction("Index", "Games");
                }

                var order = await orderService.CreateOrderAsync(user.Email, gameId, userId.Value);

                TempData["Success"] = "Покупка успешно завершена. Проверьте вашу почту для получения ключа.";

                // Если это AJAX запрос, возвращаем JSON
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, redirectUrl = Url.Action("Orders", "Account") });
                }

                // Иначе делаем обычный редирект в личный кабинет
                return RedirectToAction("Orders", "Account");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка при покупке: {ex.Message}";

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = $"Ошибка при покупке: {ex.Message}" });
                }

                return RedirectToAction("Index", "Games");
            }
        }
    }
}