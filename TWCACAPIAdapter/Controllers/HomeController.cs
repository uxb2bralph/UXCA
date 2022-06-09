using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TWCACAPIAdapter.Models;
using TWCACAPIAdapter.Models.ViewModel;
using TWCACAPIXWrapper;
using CommonLib.Utility;

namespace TWCACAPIAdapter.Controllers
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

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult TestAll()
        {
            CAPIClass capi = new CAPIClass();
            var dataSignature = capi.Sign("hello...", "", 1, 0);
            return Content("OK");
        }

        public ActionResult TestJson([FromBody]TWCASignDataViewModel viewModel)
        {
            return Content(viewModel.JsonStringify());
        }
    }
}