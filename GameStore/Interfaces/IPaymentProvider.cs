using GameStore.Models;

namespace GameStore.Interfaces
{
    public interface IPaymentProvider
    {
        string Name { get; }
        Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
        Task<PaymentStatus> CheckPaymentStatusAsync(string transactionId);
        Task<RefundResult> RefundPaymentAsync(string transactionId, decimal amount);
        Task<bool> ValidateCallbackAsync(Dictionary<string, string> parameters);
        PaymentProviderConfiguration GetConfiguration();
    }

    public class PaymentRequest
    {
        public string TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "RUB";
        public string Description { get; set; }
        public string Email { get; set; }
        public string SuccessUrl { get; set; }
        public string FailUrl { get; set; }
        public string CallbackUrl { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; }
        public string ProviderTransactionId { get; set; }
        public string RedirectUrl { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, string> AdditionalData { get; set; } = new();
    }

    public class RefundResult
    {
        public bool Success { get; set; }
        public string RefundId { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class PaymentProviderConfiguration
    {
        public Dictionary<string, string> Settings { get; set; } = new();
        public bool RequiresRedirect { get; set; } = true;
        public bool SupportsRefunds { get; set; } = true;
        public string[] SupportedCurrencies { get; set; } = new[] { "RUB" };
        public string[] SupportedMethods { get; set; } = new[] { "card", "webmoney", "yoomoney" };
    }
}