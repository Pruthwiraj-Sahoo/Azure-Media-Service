using AzureMediaServices.Models;
using AzureMediaServices.Repository;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AzureMediaServices.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        HomeRepository homeRepository;
        public HomeController(ILogger<HomeController> logger, HomeRepository home)
        {
            homeRepository = home;
            _logger = logger;
        }

        public IActionResult Index()
        {
            homeRepository.ListOfThumbnailsAsync();
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}