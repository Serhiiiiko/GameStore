using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using GameStore.Models;

namespace GameStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Browse()
        {
            // TODO: Implement browse functionality with game listings
            return View();
        }

        public IActionResult Details(int? id)
        {
            // TODO: Implement game details view with the specified ID
            if (id == null)
            {
                return NotFound();
            }

            return View();
        }

        public IActionResult Streams()
        {
            // TODO: Implement streams view
            return View();
        }

        public IActionResult Profile()
        {
            // TODO: Implement user profile view
            return View();
        }

        public IActionResult Privacy()
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