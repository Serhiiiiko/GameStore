using GameStore.Interfaces;
using GameStore.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GameStore.Services.PaymentProviders
{
    public class TestPaymentProvider : IPaymentProvider
    {
        private readonly ILogger<TestPaymentProvider> _logger;
        private readonly IConfiguration _configuration;

        public string Name => "test";

        public TestPaymentProvider(ILogger<TestPaymentProvider> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
        {
            _logger.LogInformation($"Processing test payment for transaction {request.TransactionId}");

            // Симуляция задержки обработки
            await Task.Delay(1000);

            // В тестовом режиме всегда успешный платеж
            var testProviderTransactionId = $"TEST_{Guid.NewGuid():N}";

            return new PaymentResult
            {
                Success = true,
                TransactionId = request.TransactionId,
                ProviderTransactionId = testProviderTransactionId,
                RedirectUrl = null, // Тестовый провайдер не требует редиректа
                AdditionalData = new Dictionary<string, string>
                {
                    { "test_mode", "true" },
                    { "processed_at", DateTime.UtcNow.ToString("O") }
                }
            };
        }

        public async Task<PaymentStatus> CheckPaymentStatusAsync(string transactionId)
        {
            await Task.Delay(500);
            // В тестовом режиме всегда возвращаем успешный статус
            return PaymentStatus.Completed;
        }

        public async Task<RefundResult> RefundPaymentAsync(string transactionId, decimal amount)
        {
            await Task.Delay(1000);

            return new RefundResult
            {
                Success = true,
                RefundId = $"REFUND_{Guid.NewGuid():N}",
                ErrorMessage = null
            };
        }

        public async Task<bool> ValidateCallbackAsync(Dictionary<string, string> parameters)
        {
            await Task.Delay(100);
            // В тестовом режиме всегда валидный callback
            return true;
        }

        public PaymentProviderConfiguration GetConfiguration()
        {
            return new PaymentProviderConfiguration
            {
                Settings = new Dictionary<string, string>
                {
                    { "test_mode", "true" },
                    { "auto_complete", "true" }
                },
                RequiresRedirect = false, // Тестовый провайдер не требует редиректа
                SupportsRefunds = true,
                SupportedCurrencies = new[] { "RUB", "USD", "EUR" },
                SupportedMethods = new[] { "card", "webmoney", "yoomoney" }
            };
        }
    }
}