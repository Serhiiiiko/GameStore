using System.ComponentModel.DataAnnotations;

namespace GameStore.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public int GameId { get; set; }
        public Game? Game { get; set; }

        public string Key { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; } = DateTime.Now;

        public bool IsCompleted { get; set; } = false;
    }
}