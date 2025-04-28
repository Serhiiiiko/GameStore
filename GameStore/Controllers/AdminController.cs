using GameStore.Interfaces;
using GameStore.Models;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers
{
    public class AdminController : Controller
    {
        private readonly IGameService _gameService;
        private readonly IOrderRepository _orderRepository;
        private readonly ISteamTopUpRepository _steamTopUpRepository;

        public AdminController(
            IGameService gameService,
            IOrderRepository orderRepository,
            ISteamTopUpRepository steamTopUpRepository)
        {
            _gameService = gameService;
            _orderRepository = orderRepository;
            _steamTopUpRepository = steamTopUpRepository;
        }

        // Simple authentication check for demo purposes
        private bool IsAuthenticated()
        {
            return HttpContext.Session.GetString("AdminAuthenticated") == "true";
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            // Simple auth for demo
            if (username == "admin" && password == "admin123")
            {
                HttpContext.Session.SetString("AdminAuthenticated", "true");
                return RedirectToAction("Index");
            }

            ViewBag.Error = "Неверное имя пользователя или пароль";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("AdminAuthenticated");
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Index()
        {
            if (!IsAuthenticated())
            {
                return RedirectToAction("Login");
            }

            var games = await _gameService.GetAllGamesAsync();
            return View(games);
        }

        public IActionResult Create()
        {
            if (!IsAuthenticated())
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Game game)
        {
            if (!IsAuthenticated())
            {
                return RedirectToAction("Login");
            }

            if (ModelState.IsValid)
            {
                await _gameService.CreateGameAsync(game);
                return RedirectToAction("Index");
            }

            return View(game);
        }

        public async Task<IActionResult> Edit(int id)
        {
            if (!IsAuthenticated())
            {
                return RedirectToAction("Login");
            }

            var game = await _gameService.GetGameByIdAsync(id);
            if (game == null)
            {
                return NotFound();
            }

            return View(game);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Game game)
        {
            if (!IsAuthenticated())
            {
                return RedirectToAction("Login");
            }

            if (ModelState.IsValid)
            {
                await _gameService.UpdateGameAsync(game);
                return RedirectToAction("Index");
            }

            return View(game);
        }

        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAuthenticated())
            {
                return RedirectToAction("Login");
            }

            var game = await _gameService.GetGameByIdAsync(id);
            if (game == null)
            {
                return NotFound();
            }

            return View(game);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsAuthenticated())
            {
                return RedirectToAction("Login");
            }

            await _gameService.DeleteGameAsync(id);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Orders()
        {
            if (!IsAuthenticated())
            {
                return RedirectToAction("Login");
            }

            var orders = await _orderRepository.GetAllAsync();
            return View(orders);
        }

        public async Task<IActionResult> TopUps()
        {
            if (!IsAuthenticated())
            {
                return RedirectToAction("Login");
            }

            var topUps = await _steamTopUpRepository.GetAllAsync();
            return View(topUps);
        }
    }
}