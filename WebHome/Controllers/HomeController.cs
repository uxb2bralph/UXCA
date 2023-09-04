using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebHome.Models;
using WebHome.Models.DataEntity;
using WebHome.Models.ViewModel;
using CommonLib.Utility;
using WebHome.Helper.PKI;
using Newtonsoft.Json;
using WebHome.Helper;

namespace WebHome.Controllers
{
    public class HomeController : SampleController<CA>
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, IServiceProvider serviceProvider) : base(serviceProvider)
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

        [AllowAnonymous]
        public ActionResult HandleUnknownAction(string actionName, IFormCollection forms, QueryViewModel viewModel)
        {

            ViewBag.ViewModel = viewModel;
            return View(actionName, forms);
            //this.View(actionName).ExecuteResult(this.ControllerContext);
        }

        [AllowAnonymous]
        public async Task<ActionResult> DoSignatureAsync(SignatureViewModel viewModel)
        {
            if (Request.ContentType == "application/json")
            {
                var data = await Request.GetRequestBodyAsync();
                viewModel = JsonConvert.DeserializeObject<SignatureViewModel>(data);
            }

            if (viewModel.DataSignature!= null || viewModel.SignatureAction == SignatureActionEnum.PushSignerActivation) 
            {
                return PushSignature(viewModel);
            }
            else
            {
                return PopSignature(viewModel);
            }
        }

        [AllowAnonymous]
        public ActionResult PushSignature(SignatureViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;
            viewModel.KeyID = viewModel.KeyID?.GetEfficientString();
            PKISignatureStore.PushSignature(viewModel);

            return Json(new { result = true });
        }

        [AllowAnonymous]
        public ActionResult PopSignature(SignatureViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;
            viewModel.KeyID = viewModel.KeyID?.GetEfficientString();
            SignatureViewModel? item = null;
            if (viewModel.KeyID != null)
            {
                item = PKISignatureStore.PopSignature(viewModel.KeyID);
                if (item != null)
                {
                    return Json(item);
                }
            }

            return Json(new { result = false });
        }

        [AllowAnonymous]
        public ActionResult PeekAll()
        {
            return Json(PKISignatureStore.PeekAll());
        }
    }
}