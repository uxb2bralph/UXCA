using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO;
using ContractHome.Models;
using ContractHome.Models.DataEntity;
using ContractHome.Models.ViewModel;
using CommonLib.Utility;
using Newtonsoft.Json;
using ContractHome.Helper;
using ContractHome.Properties;
using CommonLib.Core.Utility;
using System.Xml;
using GemBox.Document;
using System.Net;
using Microsoft.Extensions.Primitives;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.AspNetCore.WebUtilities;
using System.Linq.Dynamic.Core;
using ContractHome.Models.Helper;
using CommonLib.Core.AspNetMvc;
using DocumentFormat.OpenXml.Presentation;
using System.Text;

namespace ContractHome.Controllers
{
    public class AccountController : SampleController
    {
        private readonly ILogger<HomeController> _logger;

        public AccountController(ILogger<HomeController> logger, IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _logger = logger;
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult CheckLogin([FromBody]LoginViewModel viewModel)
        {
            LoginHandler login = new LoginHandler(this);
            if (!login.ProcessLogin(viewModel.PID, viewModel.Password, out string msg))
            {
                ModelState.AddModelError("PID", msg);
            }

            if (!ModelState.IsValid)
            {
                return Json(new { result = false,message = ModelState.ErrorMessage()});
            }

            return Json(new { result = true, message = Url.Action("ListToStampIndex", "ContractConsole") });
        }

        // GET: Account
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> Login(LoginViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;
            ViewBag.ModelState = this.ModelState;

            if (!ModelState.IsValid)
            {
                return View("~/Views/Account/Login.cshtml");
            }

            LoginHandler login = new LoginHandler(this);
            String msg;
            if (!login.ProcessLogin(viewModel.PID, viewModel.Password, out msg))
            {
                ModelState.AddModelError("PID", msg);
                return View("~/Views/Account/Login.cshtml");
            }

            viewModel.ReturnUrl = viewModel.ReturnUrl.GetEfficientString();
            return Redirect(viewModel.ReturnUrl ?? msg ?? "~/Account/Login");

        }

        [AllowAnonymous]
        public ActionResult Login()
        {
            this.HttpContext.Logout();

            return View("~/Views/Account/Login.cshtml");
        }

        [AllowAnonymous]
        public ActionResult Logout()
        {
            this.HttpContext.Logout();
            return RedirectToAction("Login");
        }


        public ActionResult ChangeLanguage(String lang)
        {
            var cLang = lang.GetEfficientString() ?? Settings.Default.DefaultUILanguage;
            Response.Cookies.Append("cLang", cLang);
            return Json(new { result = true, message = System.Globalization.CultureInfo.CurrentCulture.Name });
        }

        public ActionResult Error()
        {
            return View("~/Views/Shared/Error.cshtml");
        }


    }


}