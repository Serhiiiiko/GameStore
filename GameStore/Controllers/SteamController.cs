using GameStore.Interfaces;
using GameStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GameStore.Controllers
{
    public class SteamController : Controller
    {
        private readonly ISteamTopUpService _steamTopUpService;
        private readonly IAuthService _authService;
        private readonly ILogger<SteamController> _logger;

        public SteamController(
            ISteamTopUpService steamTopUpService,
            IAuthService authService,
            ILogger<SteamController> logger)
        {
            _steamTopUpService = steamTopUpService;
            _authService = authService;
            _logger = logger;
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

                // Получаем сервисы
                var paymentService = HttpContext.RequestServices.GetService<IPaymentService>();
                var steamTopUpRepository = HttpContext.RequestServices.GetService<ISteamTopUpRepository>();
                var emailService = HttpContext.RequestServices.GetService<IEmailService>();
                var telegramService = HttpContext.RequestServices.GetService<ITelegramNotificationService>();

                if (paymentService == null || steamTopUpRepository == null)
                {
                    throw new InvalidOperationException("Необходимые сервисы не настроены");
                }

                // Создаем запись о пополнении
                var topUp = new SteamTopUp
                {
                    SteamId = steamId,
                    Email = user.Email,
                    Amount = amount,
                    UserId = userId.Value,
                    Date = DateTime.UtcNow,
                    IsCompleted = false
                };

                topUp = await steamTopUpRepository.CreateAsync(topUp);

                // Создаем платежную транзакцию
                var transaction = await paymentService.CreateTransactionAsync(
                    userId.Value,
                    amount,
                    steamTopUpId: topUp.Id
                );

                // Обрабатываем платеж
                var result = await paymentService.ProcessPaymentAsync(transaction, paymentMethod);

                if (result.Success && !string.IsNullOrEmpty(result.RedirectUrl))
                {
                    // Если требуется редирект на страницу оплаты
                    return Redirect(result.RedirectUrl);
                }
                else if (result.Success)
                {
                    // Если платеж обработан без редиректа (тестовый провайдер)
                    await paymentService.UpdateTransactionStatusAsync(transaction.TransactionId, PaymentStatus.Completed);

                    // Получаем запись о пополнении из базы данных для обновления
                    var topUpToUpdate = await steamTopUpRepository.GetByIdAsync(topUp.Id);
                    if (topUpToUpdate != null)
                    {
                        topUpToUpdate.IsCompleted = true;
                        await steamTopUpRepository.UpdateAsync(topUpToUpdate);

                        // Обновляем локальный объект для отправки уведомлений
                        topUp = topUpToUpdate;
                    }

                    // Отправляем уведомления
                    if (emailService != null)
                    {
                        await emailService.SendSteamTopUpConfirmationAsync(topUp);
                    }

                    if (telegramService != null)
                    {
                        await telegramService.SendSteamTopUpNotificationAsync(topUp);
                    }

                    TempData["Success"] = "Пополнение успешно выполнено. Проверьте вашу почту для получения подтверждения.";
                    return RedirectToAction("Success", "Payment", new { transaction = transaction.TransactionId });
                }
                else
                {
                    // Ошибка платежа
                    await paymentService.UpdateTransactionStatusAsync(transaction.TransactionId, PaymentStatus.Failed);

                    TempData["Error"] = $"Ошибка при пополнении: {result.ErrorMessage}";
                    return RedirectToAction("Fail", "Payment", new { transaction = transaction.TransactionId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Steam top-up");
                TempData["Error"] = $"Ошибка при пополнении: {ex.Message}";
                return RedirectToAction("SteamTopUp", "Home");
            }
        }
    }
}