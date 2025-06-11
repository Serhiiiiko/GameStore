using System.ComponentModel.DataAnnotations;

namespace GameStore.Models
{
    public class PaymentTransaction
    {
        public int Id { get; set; }

        [Required]
        public string TransactionId { get; set; } = string.Empty; // Уникальный ID транзакции

        public int ProviderId { get; set; }
        public virtual PaymentProvider Provider { get; set; }

        public int? OrderId { get; set; }
        public virtual Order Order { get; set; }

        public int? SteamTopUpId { get; set; }
        public virtual SteamTopUp SteamTopUp { get; set; }

        public int UserId { get; set; }
        public virtual User User { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public string Currency { get; set; } = "RUB";

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public string? ProviderTransactionId { get; set; } // ID транзакции в платежной системе

        public string? PaymentMethod { get; set; } // card, webmoney, etc.

        public string? ResponseData { get; set; } // JSON ответ от платежной системы

        public string? ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public string? CallbackUrl { get; set; } // URL для callback от платежной системы

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }
    }

    public enum PaymentStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Cancelled,
        Refunded
    }
}