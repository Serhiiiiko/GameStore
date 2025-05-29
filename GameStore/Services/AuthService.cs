using GameStore.Interfaces;
using GameStore.Models;
using BCrypt.Net;

namespace GameStore.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private const string USER_SESSION_KEY = "UserId";
        private const string USER_EMAIL_KEY = "UserEmail";

        public AuthService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<User?> RegisterAsync(RegisterViewModel model)
        {
            // Check if user already exists
            if (await _userRepository.ExistsAsync(model.Email))
            {
                return null;
            }

            // Create new user
            var user = new User
            {
                Email = model.Email,
                Name = model.Name,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                RegisteredAt = DateTime.UtcNow,
                IsActive = true
            };

            return await _userRepository.CreateAsync(user);
        }

        public async Task<User?> LoginAsync(LoginViewModel model)
        {
            var user = await _userRepository.GetByEmailAsync(model.Email);

            if (user == null || !user.IsActive)
            {
                return null;
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                return null;
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            return user;
        }

        public async Task<User?> GetCurrentUserAsync(HttpContext httpContext)
        {
            var userId = GetCurrentUserId(httpContext);
            if (userId == null)
            {
                return null;
            }

            return await _userRepository.GetByIdAsync(userId.Value);
        }

        public Task LogoutAsync(HttpContext httpContext)
        {
            httpContext.Session.Remove(USER_SESSION_KEY);
            httpContext.Session.Remove(USER_EMAIL_KEY);
            return Task.CompletedTask;
        }

        public bool IsAuthenticated(HttpContext httpContext)
        {
            return httpContext.Session.GetInt32(USER_SESSION_KEY) != null;
        }

        public int? GetCurrentUserId(HttpContext httpContext)
        {
            return httpContext.Session.GetInt32(USER_SESSION_KEY);
        }
    }
}