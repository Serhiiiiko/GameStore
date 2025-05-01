using GameStore.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers
{
    public class GamesController : Controller
    {
        private readonly IGameService _gameService;

        public GamesController(IGameService gameService)
        {
            _gameService = gameService;
        }

        public async Task<IActionResult> Index()
        {
            var games = await _gameService.GetAllGamesAsync();
            return View(games);
        }

        public async Task<IActionResult> Details(int id)
        {
            var game = await _gameService.GetGameByIdAsync(id);
            if (game == null)
            {
                return NotFound();
            }
            return View(game);
        }
        // In GameStore/Controllers/GamesController.cs
        [HttpPost]
        public async Task<IActionResult> Purchase(int gameId, string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Пожалуйста, укажите email";
                return RedirectToAction("Details", new { id = gameId });
            }

            try
            {
                var orderService = HttpContext.RequestServices.GetService<IOrderService>();
                if (orderService == null)
                {
                    TempData["Error"] = "Сервис заказа недоступен.";
                    return RedirectToAction("Details", new { id = gameId });
                }

                var order = await orderService.CreateOrderAsync(email, gameId);

                // Store order ID in cookie
                var orderIds = Request.Cookies["UserOrders"]?.Split(',').ToList() ?? new List<string>();
                orderIds.Add(order.Id.ToString());

                // Save updated order IDs back to cookie (expires in 30 days)
                Response.Cookies.Append("UserOrders", string.Join(",", orderIds), new CookieOptions
                {
                    Expires = DateTime.Now.AddDays(30),
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax
                });

                TempData["Success"] = "Покупка успешно завершена. Проверьте вашу почту для получения ключа.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка при покупке: {ex.Message}";
            }

            // Changed from redirecting to Details to redirecting to Home page
            return RedirectToAction("Index", "Home");
        }
    }
}