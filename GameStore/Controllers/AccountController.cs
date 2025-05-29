using GameStore.Interfaces;
using GameStore.Models;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IOrderRepository _orderRepository;
        private readonly ISteamTopUpRepository _steamTopUpRepository;

        public AccountController(
            IAuthService authService,
            IOrderRepository orderRepository,
            ISteamTopUpRepository steamTopUpRepository)
        {
            _authService = authService;
            _orderRepository = orderRepository;
            _steamTopUpRepository = steamTopUpRepository;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (_authService.IsAuthenticated(HttpContext))
            {
                return RedirectToAction("Dashboard");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _authService.LoginAsync(model);
            if (user == null)
            {
                ModelState.AddModelError("", "Неверный email или пароль");
                return View(model);
            }

            // Set session
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserName", user.Name);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (_authService.IsAuthenticated(HttpContext))
            {
                return RedirectToAction("Dashboard");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _authService.RegisterAsync(model);
            if (user == null)
            {
                ModelState.AddModelError("Email", "Пользователь с таким email уже существует");
                return View(model);
            }

            // Auto login after registration
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserName", user.Name);

            TempData["Success"] = "Регистрация прошла успешно! Добро пожаловать!";
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync(HttpContext);
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Dashboard()
        {
            if (!_authService.IsAuthenticated(HttpContext))
            {
                return RedirectToAction("Login");
            }

            var user = await _authService.GetCurrentUserAsync(HttpContext);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            ViewBag.TotalOrders = user.Orders.Count;
            ViewBag.TotalTopUps = user.SteamTopUps.Count;
            ViewBag.TotalSpent = user.Orders.Sum(o => o.Game?.Price ?? 0) + user.SteamTopUps.Sum(t => t.Amount);

            return View(user);
        }

        public async Task<IActionResult> Orders()
        {
            if (!_authService.IsAuthenticated(HttpContext))
            {
                return RedirectToAction("Login");
            }

            var user = await _authService.GetCurrentUserAsync(HttpContext);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            return View(user.Orders.OrderByDescending(o => o.OrderDate));
        }

        public async Task<IActionResult> TopUps()
        {
            if (!_authService.IsAuthenticated(HttpContext))
            {
                return RedirectToAction("Login");
            }

            var user = await _authService.GetCurrentUserAsync(HttpContext);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            return View(user.SteamTopUps.OrderByDescending(t => t.Date));
        }
    }
}