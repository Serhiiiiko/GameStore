using GameStore.Interfaces;
using GameStore.Models;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly IPaymentProviderFactory _providerFactory;
        private readonly IOrderService _orderService;
        private readonly ISteamTopUpService _steamTopUpService;
        private readonly IAuthService _authService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IPaymentService paymentService,
            IPaymentProviderFactory providerFactory,
            IOrderService orderService,
            ISteamTopUpService steamTopUpService,
            IAuthService authService,
            ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _providerFactory = providerFactory;
            _orderService = orderService;
            _steamTopUpService = steamTopUpService;
            _authService = authService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Success(string transaction)
        {
            var paymentTransaction = await _paymentService.GetTransactionAsync(transaction);
            if (paymentTransaction == null)
            {
                return NotFound();
            }

            ViewBag.Transaction = paymentTransaction;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Fail(string transaction)
        {
            var paymentTransaction = await _paymentService.GetTransactionAsync(transaction);
            ViewBag.Transaction = paymentTransaction;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Callback(string provider, [FromForm] Dictionary<string, string> parameters)
        {
            try
            {
                _logger.LogInformation($"Received callback from {provider}: {string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}"))}");

                var paymentProvider = _providerFactory.GetProvider(provider);

                if (await paymentProvider.ValidateCallbackAsync(parameters))
                {
                    // Обработка успешного callback
                    // Обновление статуса транзакции
                    return Ok();
                }

                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing callback from {provider}");
                return StatusCode(500);
            }
        }

        public async Task<IActionResult> History()
        {
            if (!_authService.IsAuthenticated(HttpContext))
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = _authService.GetCurrentUserId(HttpContext);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var transactions = await _paymentService.GetUserTransactionsAsync(userId.Value);
            return View(transactions);
        }
    }
}