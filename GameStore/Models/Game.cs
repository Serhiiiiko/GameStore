using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace GameStore.Models
{
    public class Game
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        public string Genre { get; set; } = string.Empty;

        [Required]
        public decimal Price { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        public double Rating { get; set; }

        public string Downloads { get; set; } = string.Empty;

        [Required]
        public PlatformType Platform { get; set; } = PlatformType.Steam;

        [NotMapped] // Это свойство не будет сохраняться в базе данных
        public IFormFile ImageFile { get; set; }
    }
}