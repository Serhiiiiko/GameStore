using GameStore.Data;
using GameStore.Interfaces;
using GameStore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GameStore.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPaymentProviderFactory _providerFactory;
        private readonly ILogger<PaymentService> _logger;
        private readonly IConfiguration _configuration;

        public PaymentService(
            ApplicationDbContext context,
            IPaymentProviderFactory providerFactory,
            ILogger<PaymentService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _providerFactory = providerFactory;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<PaymentProvider> GetActiveProviderAsync()
        {
            return await _context.PaymentProviders
                .FirstOrDefaultAsync(p => p.IsActive && p.IsDefault);
        }

        public async Task<IEnumerable<PaymentProvider>> GetAllProvidersAsync()
        {
            return await _context.PaymentProviders
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<PaymentProvider> GetProviderByIdAsync(int id)
        {
            return await _context.PaymentProviders.FindAsync(id);
        }

        public async Task<PaymentProvider> CreateProviderAsync(PaymentProvider provider)
        {
            provider.CreatedAt = DateTime.UtcNow;
            _context.PaymentProviders.Add(provider);
            await _context.SaveChangesAsync();
            return provider;
        }

        public async Task UpdateProviderAsync(PaymentProvider provider)
        {
            provider.UpdatedAt = DateTime.UtcNow;
            _context.Entry(provider).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task<bool> SetActiveProviderAsync(int providerId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Деактивируем все провайдеры
                await _context.PaymentProviders
                    .Where(p => p.IsDefault)
                    .ExecuteUpdateAsync(p => p.SetProperty(x => x.IsDefault, false));

                // Активируем выбранный провайдер
                var provider = await _context.PaymentProviders.FindAsync(providerId);
                if (provider == null) return false;

                provider.IsDefault = true;
                provider.IsActive = true;
                provider.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting active provider {providerId}");
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<PaymentTransaction> CreateTransactionAsync(
            int userId,
            decimal amount,
            int? orderId = null,
            int? steamTopUpId = null)
        {
            var activeProvider = await GetActiveProviderAsync();
            if (activeProvider == null)
            {
                throw new InvalidOperationException("No active payment provider configured");
            }

            var transaction = new PaymentTransaction
            {
                TransactionId = GenerateTransactionId(),
                ProviderId = activeProvider.Id,
                UserId = userId,
                OrderId = orderId,
                SteamTopUpId = steamTopUpId,
                Amount = amount,
                Currency = "RUB",
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.PaymentTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            return transaction;
        }

        public async Task<PaymentResult> ProcessPaymentAsync(PaymentTransaction transaction, string paymentMethod)
        {
            try
            {
                var provider = await _context.PaymentProviders.FindAsync(transaction.ProviderId);
                if (provider == null || !provider.IsActive)
                {
                    throw new InvalidOperationException("Payment provider not available");
                }

                var paymentProvider = _providerFactory.GetProvider(provider.Name);

                var baseUrl = _configuration["BaseUrl"] ?? "https://localhost:7298";

                var request = new PaymentRequest
                {
                    TransactionId = transaction.TransactionId,
                    Amount = transaction.Amount,
                    Currency = transaction.Currency,
                    Email = (await _context.Users.FindAsync(transaction.UserId))?.Email ?? "",
                    Description = GetTransactionDescription(transaction),
                    SuccessUrl = $"{baseUrl}/Payment/Success",
                    FailUrl = $"{baseUrl}/Payment/Fail",
                    CallbackUrl = $"{baseUrl}/Payment/Callback/{provider.Name}",
                    Metadata = new Dictionary<string, string>
                    {
                        { "payment_method", paymentMethod },
                        { "user_id", transaction.UserId.ToString() }
                    }
                };

                transaction.PaymentMethod = paymentMethod;
                transaction.Status = PaymentStatus.Processing;

                var result = await paymentProvider.ProcessPaymentAsync(request);

                if (result.Success)
                {
                    transaction.ProviderTransactionId = result.ProviderTransactionId;
                    transaction.ResponseData = JsonSerializer.Serialize(result.AdditionalData);
                }
                else
                {
                    transaction.Status = PaymentStatus.Failed;
                    transaction.ErrorMessage = result.ErrorMessage;
                }

                await _context.SaveChangesAsync();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing payment for transaction {transaction.TransactionId}");

                transaction.Status = PaymentStatus.Failed;
                transaction.ErrorMessage = ex.Message;
                await _context.SaveChangesAsync();

                return new PaymentResult
                {
                    Success = false,
                    TransactionId = transaction.TransactionId,
                    ErrorMessage = "Ошибка при обработке платежа"
                };
            }
        }

        public async Task<PaymentTransaction> GetTransactionAsync(string transactionId)
        {
            return await _context.PaymentTransactions
                .Include(t => t.Provider)
                .Include(t => t.User)
                .Include(t => t.Order)
                    .ThenInclude(o => o.Game)
                .Include(t => t.SteamTopUp)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);
        }

        public async Task UpdateTransactionStatusAsync(
            string transactionId,
            PaymentStatus status,
            string providerTransactionId = null)
        {
            var transaction = await _context.PaymentTransactions
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

            if (transaction != null)
            {
                transaction.Status = status;

                if (!string.IsNullOrEmpty(providerTransactionId))
                {
                    transaction.ProviderTransactionId = providerTransactionId;
                }

                if (status == PaymentStatus.Completed)
                {
                    transaction.CompletedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<PaymentTransaction>> GetUserTransactionsAsync(int userId)
        {
            return await _context.PaymentTransactions
                .Include(t => t.Provider)
                .Include(t => t.Order)
                    .ThenInclude(o => o.Game)
                .Include(t => t.SteamTopUp)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<PaymentTransaction>> GetAllTransactionsAsync()
        {
            return await _context.PaymentTransactions
                .Include(t => t.Provider)
                .Include(t => t.User)
                .Include(t => t.Order)
                    .ThenInclude(o => o.Game)
                .Include(t => t.SteamTopUp)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        private string GenerateTransactionId()
        {
            return $"TRX_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}".Substring(0, 30);
        }

        private string GetTransactionDescription(PaymentTransaction transaction)
        {
            if (transaction.OrderId.HasValue)
            {
                return $"Покупка игры #{transaction.OrderId}";
            }
            else if (transaction.SteamTopUpId.HasValue)
            {
                return $"Пополнение Steam #{transaction.SteamTopUpId}";
            }

            return "Платеж в GameStore";
        }
    }
}