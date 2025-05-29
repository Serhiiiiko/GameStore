using System.Diagnostics;
using GameStore.Interfaces;
using GameStore.Models;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly IGameService _gameService;
        private readonly IOrderRepository _orderRepository;
        private readonly IAuthService _authService;

        public HomeController(
            IGameService gameService,
            IOrderRepository orderRepository,
            IAuthService authService)
        {
            _gameService = gameService;
            _orderRepository = orderRepository;
            _authService = authService;
        }

        public async Task<IActionResult> Index()
        {
            var games = await _gameService.GetAllGamesAsync();

            // Check if user is authenticated
            if (_authService.IsAuthenticated(HttpContext))
            {
                var user = await _authService.GetCurrentUserAsync(HttpContext);
                if (user != null)
                {
                    ViewBag.UserOrders = user.Orders.OrderByDescending(o => o.OrderDate).ToList();
                }
            }
            else
            {
                ViewBag.UserOrders = new List<Order>();
            }

            ViewBag.IsAuthenticated = _authService.IsAuthenticated(HttpContext);

            return View(games);
        }

        public IActionResult SteamTopUp()
        {
            ViewBag.IsAuthenticated = _authService.IsAuthenticated(HttpContext);
            return View();
        }

        public IActionResult PurchaseHistory()
        {
            // Redirect to Account Dashboard if authenticated
            if (_authService.IsAuthenticated(HttpContext))
            {
                return RedirectToAction("Orders", "Account");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> PurchaseHistoryByEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Пожалуйста, введите email";
                return View("PurchaseHistory");
            }

            var orders = await _orderRepository.GetByEmailAsync(email);
            return View("PurchaseHistoryResult", orders);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult UserAgreement()
        {
            return View();
        }

        public IActionResult PaymentAndDelivery()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}