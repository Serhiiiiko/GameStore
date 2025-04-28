using System.ComponentModel.DataAnnotations;

namespace GameStore.Models
{
    public class AdminUser
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = "Admin";
    }
}