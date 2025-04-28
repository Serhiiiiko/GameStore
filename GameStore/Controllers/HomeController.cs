using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using GameStore.Models;
using GameStore.Interfaces;

namespace GameStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IGameService _gameService;

        public HomeController(ILogger<HomeController> logger, IGameService gameService)
        {
            _logger = logger;
            _gameService = gameService;
        }

        public async Task<IActionResult> Index()
        {
            var games = await _gameService.GetAllGamesAsync();
            return View(games);
        }

        public async Task<IActionResult> Browse()
        {
            var games = await _gameService.GetAllGamesAsync();
            return View(games);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var game = await _gameService.GetGameByIdAsync(id.Value);
            if (game == null)
            {
                return NotFound();
            }

            return View(game);
        }

        public IActionResult Streams()
        {
            return View();
        }

        public IActionResult Profile(string email)
        {
            ViewBag.Email = email;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public async Task<IActionResult> PurchaseHistory()
        {
            // Get order IDs from cookie
            string orderIdsCookie = Request.Cookies["UserOrders"] ?? "";
            List<int> orderIds = new List<int>();

            if (!string.IsNullOrEmpty(orderIdsCookie))
            {
                // Parse order IDs from cookie
                foreach (var idStr in orderIdsCookie.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(idStr, out int id))
                    {
                        orderIds.Add(id);
                    }
                }
            }

            // If we have orders in cookies, fetch them
            if (orderIds.Any())
            {
                var orderRepository = HttpContext.RequestServices.GetService<IOrderRepository>();
                var orders = new List<Order>();

                foreach (var id in orderIds)
                {
                    var order = await orderRepository.GetByIdAsync(id);
                    if (order != null)
                    {
                        orders.Add(order);
                    }
                }

                return View("PurchaseHistoryResult", orders);
            }

            // If no orders in cookies, show form to enter email as fallback
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> PurchaseHistoryByEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Пожалуйста, укажите email";
                return View("PurchaseHistory");
            }

            var orderService = HttpContext.RequestServices.GetService<IOrderService>();
            var orders = await orderService.GetOrdersByEmailAsync(email);

            return View("PurchaseHistoryResult", orders);
        }
        public IActionResult SteamTopUp()
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