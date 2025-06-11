using GameStore.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GameStore.Controllers
{
    public class GamesController : Controller
    {
        private readonly IGameService _gameService;
        private readonly IAuthService _authService;
        private readonly ILogger<GamesController> _logger;

        public GamesController(
            IGameService gameService,
            IAuthService authService,
            ILogger<GamesController> logger)
        {
            _gameService = gameService;
            _authService = authService;
            _logger = logger;
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
                var game = await _gameService.GetGameByIdAsync(gameId);
                if (game == null)
                {
                    throw new Exception("Игра не найдена");
                }

                // Получаем сервисы через ServiceProvider
                var paymentService = HttpContext.RequestServices.GetService<IPaymentService>();
                var orderRepository = HttpContext.RequestServices.GetService<IOrderRepository>();
                var orderService = HttpContext.RequestServices.GetService<IOrderService>();
                var emailService = HttpContext.RequestServices.GetService<IEmailService>();
                var telegramService = HttpContext.RequestServices.GetService<ITelegramNotificationService>();

                if (paymentService == null || orderRepository == null || orderService == null)
                {
                    throw new InvalidOperationException("Необходимые сервисы не настроены");
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

                // Создаем заказ
                var order = new Models.Order
                {
                    GameId = gameId,
                    UserId = userId.Value,
                    Email = user.Email,
                    OrderDate = DateTime.UtcNow,
                    IsCompleted = false
                };

                // Временно сохраняем заказ
                order = await orderRepository.CreateAsync(order);

                // Создаем транзакцию
                var transaction = await paymentService.CreateTransactionAsync(
                    userId.Value,
                    game.Price,
                    orderId: order.Id
                );

                // Получаем метод оплаты из формы
                var paymentMethod = Request.Form["paymentMethod"].ToString();
                if (string.IsNullOrEmpty(paymentMethod))
                {
                    paymentMethod = "card"; // По умолчанию
                }

                // Обрабатываем платеж
                var result = await paymentService.ProcessPaymentAsync(transaction, paymentMethod);

                if (result.Success && !string.IsNullOrEmpty(result.RedirectUrl))
                {
                    // Если требуется редирект на страницу оплаты
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true, redirectUrl = result.RedirectUrl });
                    }

                    return Redirect(result.RedirectUrl);
                }
                else if (result.Success)
                {
                    // Если платеж обработан без редиректа (тестовый провайдер)
                    await paymentService.UpdateTransactionStatusAsync(transaction.TransactionId, Models.PaymentStatus.Completed);

                    // Завершаем заказ
                    order.Key = await orderService.GenerateGameKeyAsync();
                    order.IsCompleted = true;
                    await orderRepository.UpdateAsync(order);

                    // Загружаем связанную игру для уведомлений
                    order.Game = game;

                    // Отправляем email и уведомления
                    if (emailService != null)
                    {
                        await emailService.SendOrderConfirmationAsync(order);
                    }

                    if (telegramService != null)
                    {
                        await telegramService.SendGamePurchaseNotificationAsync(order);
                    }

                    TempData["Success"] = "Покупка успешно завершена. Проверьте вашу почту для получения ключа.";

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true, redirectUrl = Url.Action("Success", "Payment", new { transaction = transaction.TransactionId }) });
                    }

                    return RedirectToAction("Success", "Payment", new { transaction = transaction.TransactionId });
                }
                else
                {
                    // Ошибка платежа
                    await paymentService.UpdateTransactionStatusAsync(transaction.TransactionId, Models.PaymentStatus.Failed);

                    TempData["Error"] = $"Ошибка при оплате: {result.ErrorMessage}";

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = result.ErrorMessage });
                    }

                    return RedirectToAction("Fail", "Payment", new { transaction = transaction.TransactionId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during purchase");
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