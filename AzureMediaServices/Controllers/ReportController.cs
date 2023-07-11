using AzureMediaServices.Repository;
using Microsoft.AspNetCore.Mvc;

namespace AzureMediaServices.Controllers
{
    public class ReportController : Controller
    {
        ReportRepository _report;
        public ReportController(ReportRepository report)
        {
            _report = report;
        }
        public IActionResult Index()
        {
            return View();
        }
    }
}
