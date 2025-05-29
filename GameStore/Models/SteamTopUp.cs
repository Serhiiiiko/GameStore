using System.ComponentModel.DataAnnotations;

namespace GameStore.Models
{
    public class SteamTopUp
    {
        public int Id { get; set; }

        [Required]
        public string SteamId { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public decimal Amount { get; set; }

        // Add User relationship
        public int? UserId { get; set; }
        public User? User { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        public bool IsCompleted { get; set; } = false;
    }
}