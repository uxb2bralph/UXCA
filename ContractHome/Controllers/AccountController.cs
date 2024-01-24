using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Dynamic.Core;
using System.Drawing;
using System.Drawing.Imaging;
using ContractHome.Models.DataEntity;
using ContractHome.Models.ViewModel;
using ContractHome.Helper;
using ContractHome.Properties;
using CommonLib.Utility;
//using Microsoft.Extensions.Primitives;
using Color = System.Drawing.Color;
using ContractHome.Models.Email.Template;
using ContractHome.Models.Email;

namespace ContractHome.Controllers
{
    public class AccountController : SampleController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IMailService _mailService;
        private readonly EmailBody _emailBody;
        private readonly EmailFactory _emailFactory;

        public AccountController(ILogger<HomeController> logger, IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _logger = logger;
            _mailService = ServiceProvider.GetRequiredService<IMailService>();
            _emailFactory = serviceProvider.GetRequiredService<EmailFactory>();
            _emailBody = serviceProvider.GetRequiredService<EmailBody>();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> CheckLogin([FromBody] LoginViewModel viewModel)
        {
            LoginHandler login = new LoginHandler(this);
            if (!login.ProcessLogin(viewModel.PID, viewModel.Password, out string msg))
            {
                ModelState.AddModelError("PID", msg);
            }

            var userprofile = models.GetTable<UserProfile>().Where(x => x.PID == viewModel.PID).FirstOrDefault();
            var userOrg = userprofile?.OrganizationUser.Organization ?? null;

            if (!ModelState.IsValid)
            {
                //wait to do...甲方的公司UserEmail登入失敗通知信

                if (userOrg != null && userOrg.CanCreateContract == true)
                {
                    var emailBody =
                        new EmailBodyBuilder(_emailBody)
                        .SetTemplateItem(EmailBody.EmailTemplate.LoginFailed)
                        .SetUserName(userprofile.UserName)
                        .SetUserEmail(userprofile.EMail)
                    .Build();

                    var emailData = _emailFactory.GetEmailToCustomer(
                        emailBody.UserEmail,
                        _emailFactory.GetEmailTitle(EmailBody.EmailTemplate.LoginFailed),
                        await emailBody.GetViewRenderString());

                    _mailService?.SendMailAsync(emailData, default);
                }

                return Json(new { result = false, message = ModelState.ErrorMessage() });
            }

            if (userOrg != null && userOrg.CanCreateContract == true)
            {
                var emailBody =
                new EmailBodyBuilder(_emailBody)
                .SetTemplateItem(EmailBody.EmailTemplate.LoginSuccessed)
                .SetUserName(userprofile.UserName)
                .SetUserEmail(userprofile.EMail)
                .Build();

                var emailData = _emailFactory.GetEmailToCustomer(
                    emailBody.UserEmail,
                    _emailFactory.GetEmailTitle(EmailBody.EmailTemplate.LoginSuccessed),
                    await emailBody.GetViewRenderString());

                _mailService?.SendMailAsync(emailData, default);
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

            //wait to do...甲方的公司ContactEmail&&UserEmail登入成功通知信

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

        [AllowAnonymous]
        public async Task<ActionResult> CaptchaImgAsync(String code)
        {

            string captcha = code.DecryptData();

            Response.Clear();
            Response.ContentType = "image/Png";
            using (Bitmap bmp = new Bitmap(120, 30))
            {
                int x1 = 0;
                int y1 = 0;
                int x2 = 0;
                int y2 = 0;
                int x3 = 0;
                int y3 = 0;
                int intNoiseWidth = 25;
                int intNoiseHeight = 15;
                Random rdn = new Random();
                using (Graphics g = Graphics.FromImage(bmp))
                {

                    //設定字型
                    using (System.Drawing.Font font = new System.Drawing.Font("Courier New", 16, FontStyle.Bold))
                    {

                        //設定圖片背景
                        g.Clear(Color.CadetBlue);

                        //產生雜點
                        for (int i = 0; i < 100; i++)
                        {
                            x1 = rdn.Next(0, bmp.Width);
                            y1 = rdn.Next(0, bmp.Height);
                            bmp.SetPixel(x1, y1, Color.DarkGreen);
                        }

                        using (Pen pen = new Pen(Brushes.Gray))
                        {
                            //產生擾亂弧線
                            for (int i = 0; i < 15; i++)
                            {
                                x1 = rdn.Next(bmp.Width - intNoiseWidth);
                                y1 = rdn.Next(bmp.Height - intNoiseHeight);
                                x2 = rdn.Next(1, intNoiseWidth);
                                y2 = rdn.Next(1, intNoiseHeight);
                                x3 = rdn.Next(0, 45);
                                y3 = rdn.Next(-270, 270);
                                g.DrawArc(pen, x1, y1, x2, y2, x3, y3);
                            }
                        }

                        //把GenPassword()方法換成你自己的密碼產生器，記得把產生出來的密碼存起來日後才能與user的輸入做比較。

                        g.DrawString(captcha, font, Brushes.Black, 3, 3);

                        using (MemoryStream ms = new MemoryStream())
                        {
                            bmp.Save(ms, ImageFormat.Png);
                            byte[] bmpBytes = ms.GetBuffer();
                            //bmp.Dispose();
                            //ms.Close();
                            await Response.Body.WriteAsync(bmpBytes);
                        }

                        //context.Response.End();
                    }
                }
            }

            return new EmptyResult();
        }

    }


}