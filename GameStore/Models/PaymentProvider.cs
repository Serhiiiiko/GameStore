using System.ComponentModel.DataAnnotations;

namespace GameStore.Models
{
    public class PaymentProvider
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string DisplayName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Icon { get; set; } = string.Empty; // Font Awesome class или URL изображения

        public bool IsActive { get; set; }

        public bool IsDefault { get; set; }

        // Настройки провайдера в JSON формате
        public string Configuration { get; set; } = "{}";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Навигационные свойства
        public virtual ICollection<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();
    }
}