using Microsoft.AspNetCore.Http;

namespace GameStore.Interfaces
{
    public interface IFileService
    {
        Task<string> SaveImageAsync(IFormFile file);
        void DeleteImage(string fileName);
    }
}