using GameStore.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace GameStore.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly string _uploadsFolder;

        public FileService(IWebHostEnvironment environment)
        {
            _environment = environment;
            _uploadsFolder = Path.Combine(_environment.WebRootPath, "images");

            // Создаем директорию, если она не существует
            if (!Directory.Exists(_uploadsFolder))
            {
                Directory.CreateDirectory(_uploadsFolder);
            }
        }

        public async Task<string> SaveImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;

            // Проверяем, что файл - изображение
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !IsImageExtension(extension))
                throw new ArgumentException("Загруженный файл не является изображением");

            // Генерируем уникальное имя файла
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(_uploadsFolder, fileName);

            // Сохраняем файл
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Возвращаем относительный путь к файлу для сохранения в БД
            return $"/images/{fileName}";
        }

        public void DeleteImage(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            try
            {
                var fileName = Path.GetFileName(filePath);
                var fullPath = Path.Combine(_uploadsFolder, fileName);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch (Exception)
            {
                // Логирование ошибки (в реальном приложении)
            }
        }

        private bool IsImageExtension(string extension)
        {
            return extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".gif";
        }
    }
}