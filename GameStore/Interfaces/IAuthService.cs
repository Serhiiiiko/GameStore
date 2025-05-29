using GameStore.Models;

namespace GameStore.Interfaces
{
    public interface IAuthService
    {
        Task<User?> RegisterAsync(RegisterViewModel model);
        Task<User?> LoginAsync(LoginViewModel model);
        Task<User?> GetCurrentUserAsync(HttpContext httpContext);
        Task LogoutAsync(HttpContext httpContext);
        bool IsAuthenticated(HttpContext httpContext);
        int? GetCurrentUserId(HttpContext httpContext);
    }
}