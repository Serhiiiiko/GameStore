using GameStore.Models;

namespace GameStore.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentProvider> GetActiveProviderAsync();
        Task<IEnumerable<PaymentProvider>> GetAllProvidersAsync();
        Task<PaymentProvider> GetProviderByIdAsync(int id);
        Task<PaymentProvider> CreateProviderAsync(PaymentProvider provider);
        Task UpdateProviderAsync(PaymentProvider provider);
        Task<bool> SetActiveProviderAsync(int providerId);
        Task<PaymentTransaction> CreateTransactionAsync(int userId, decimal amount, int? orderId = null, int? steamTopUpId = null);
        Task<PaymentResult> ProcessPaymentAsync(PaymentTransaction transaction, string paymentMethod);
        Task<PaymentTransaction> GetTransactionAsync(string transactionId);
        Task UpdateTransactionStatusAsync(string transactionId, PaymentStatus status, string providerTransactionId = null);
        Task<IEnumerable<PaymentTransaction>> GetUserTransactionsAsync(int userId);
        Task<IEnumerable<PaymentTransaction>> GetAllTransactionsAsync();
    }
}