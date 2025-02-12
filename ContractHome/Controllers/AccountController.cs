using CommonLib.Core.Utility;
using CommonLib.Utility;
using ContractHome.Helper;
using ContractHome.Helper.Security.MembershipManagement;
using ContractHome.Models.Cache;
using ContractHome.Models.DataEntity;
using ContractHome.Models.Dto;
using ContractHome.Models.Email.Template;
using ContractHome.Models.Helper;
using ContractHome.Models.ViewModel;
using ContractHome.Properties;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq.Dynamic.Core;
using System.Text.RegularExpressions;
using static ContractHome.Helper.JwtTokenGenerator;
using Color = System.Drawing.Color;

namespace ContractHome.Controllers
{
    public class AccountController : SampleController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly EmailFactory _emailFactory;
        private ContractServices? _contractServices;
        private readonly ICacheStore _cacheStore;
        public AccountController(ILogger<HomeController> logger,
            IServiceProvider serviceProvider,
            EmailFactory emailContentFactories,
            ContractServices contractServices,
            ICacheStore cacheStore
            ) : base(serviceProvider)
        {
            _logger = logger;
            _emailFactory = emailContentFactories;
            _contractServices = contractServices;
            _cacheStore = cacheStore;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> CheckLogin([FromBody] LoginViewModel viewModel)
        {
            LoginHandler login = new LoginHandler(this);
            var userprofile = models.GetTable<UserProfile>().Where(x => x.PID == viewModel.PID).FirstOrDefault();
            if (!login.ProcessLogin(viewModel.PID, viewModel.Password, out string msg))
            {
                return Json(new { result = false, message = msg });
            }

            if (!ModelState.IsValid)
            {
                //wait to do...甲方的公司UserEmail登入失敗通知信
                if (userprofile.CanCreateContract)
                {
                    _emailFactory.SendEmailToCustomer(
                        _emailFactory.GetLoginFailed(emailUserName: userprofile.PID, email: userprofile.EMail));
                }

                return Json(new { result = false, message = ModelState.ErrorMessage() });
            }

            if (userprofile.CanCreateContract)
            {
                _emailFactory.SendEmailToCustomer(
                    _emailFactory.GetLoginSuccessed(emailUserName: userprofile.PID, email: userprofile.EMail));
            }

            //var dateNeedToUpdatePassword = DateTime.Now.AddMonths(-3);
            //var res = DateTime.Compare(userprofile.PasswordUpdatedDate ?? dateNeedToUpdatePassword, dateNeedToUpdatePassword);
            //if (res <= 0 && !userprofile.IsSysAdmin())
            //{
            //    return Json(new { result = true, message = Url.Action("PasswordChangeView", "UserProfile") });
            //}
            return Json(new { result = true, message = Url.Action("ListToStampIndex", "Task") });

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

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult> Trust(string token)
        {
            Logout();
            token = token.GetEfficientString();
            //var cahceKey = _cacheFactory.GetTokenCache(token);
            TokenCache cacheKey = new TokenCache(token);
            Token tokenCache = this._cacheStore.Get(cacheKey);
            if (tokenCache != null)
            {
                TempData["message"] += $"Token已失效，請重新申請。";
            }

            var tokenBase64UrlDecode = JwtTokenValidator.Base64UrlDecodeToString(token);
            _contractServices.SetModels(models);
            (BaseResponse resp, JwtToken jwtTokenObj, UserProfile userProfile) =
                _contractServices.TokenValidate(tokenBase64UrlDecode.DecryptData());

            if (resp.HasError)
            {
                FileLogger.Logger.Error($"{resp.Message}-{this.GetType().Name}-Base64Token={token}");
                TempData["message"] += resp.Message;
            }

            PasswordResetViewModel passwordResetViewModel = new PasswordResetViewModel() { Token = token };
            return View("PasswordReset", passwordResetViewModel);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<BaseResponse> PasswordReset([FromBody] PasswordResetViewModel viewModel)
        {
            var token = viewModel.Token.GetEfficientString();
            var password = viewModel.Password.GetEfficientString();
            var pid = viewModel.PID.GetEfficientString();
            TokenCache cacheKey = new TokenCache(token);
            Token tokenCache = this._cacheStore.Get(cacheKey);
            if (tokenCache != null)
            {
                return new BaseResponse(true, $"Token已失效，請重新申請。");
            }

            _contractServices.SetModels(models);
            (BaseResponse resp, JwtToken jwtTokenObj, UserProfile tokenUserProfile)
                = _contractServices.TokenValidate(JwtTokenValidator.Base64UrlDecodeToString(token).DecryptData());

            if (tokenUserProfile.PID.Equals("ifsadmin"))
            {
                return new BaseResponse(true, $"變更失敗。");
            }

            if (!tokenUserProfile.PID.Equals(pid))
            {
                return new BaseResponse(true, $"驗證資料有誤(token)。");
            }

            if (resp.HasError)
            {
                FileLogger.Logger.Error($"{resp.Message}-{this.GetType().Name}-Base64Token={token}");
                return resp;
            }

            var isPasswordValid = Regex.IsMatch(password, UserProfileFactory.PasswordRegex);
            if (!isPasswordValid)
            {
                return new BaseResponse(true, $"新密碼不符合格式");
            }

            var viewModelUserProfile
              = models.GetTable<UserProfile>()
                  .Where(x => x.UID.Equals(jwtTokenObj.DecryptUID))
                  .Where(x => x.PID.Equals(pid))
                  .FirstOrDefault();

            if (viewModelUserProfile == null)
            {
                return new BaseResponse(true, "驗證資料有誤(user)。");
            }

            tokenUserProfile.Password = null;
            tokenUserProfile.LoginFailedCount = 0;
            tokenUserProfile.Password2 = password.HashPassword();
            tokenUserProfile.PasswordUpdatedDate = DateTime.Now;

            models.SubmitChanges();

            //_cacheFactory.SetTokenCache(token);
            _cacheStore.Add(new Token(), cacheKey);

            _emailFactory.SendEmailToCustomer(
                _emailFactory.GetPasswordUpdated(emailUserName: tokenUserProfile.PID, email: tokenUserProfile.EMail));

            Logout();
            return new BaseResponse(false, "密碼更新完成。");
        }

        [AllowAnonymous]
        [HttpGet]
        public Task<BaseResponse> GetPasswordApply(string email)
        {
            return PasswordApply(new PasswordResetViewModel() { Email = email });
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<BaseResponse> PasswordApply([FromBody] PasswordResetViewModel viewModel)
        {

            UserProfile userProfile;

            if (!string.IsNullOrEmpty(viewModel.UID))
            {
                userProfile
                    = models.GetTable<UserProfile>()
                          .Where(x => x.UID.Equals(viewModel.UID.DecryptKeyValue()))
                          .FirstOrDefault();
            } 
            else
            {
                var email = viewModel.Email.GetEfficientString();
                if (string.IsNullOrEmpty(email))
                {
                    return new BaseResponse(true, "驗證資料有誤，請檢查輸入欄位是否正確。");
                }

                userProfile
                    = models.GetTable<UserProfile>()
                          .Where(x => x.EMail.Equals(email))
                          .Where(x => x.PID.Equals(viewModel.PID))
                          .FirstOrDefault();
            }

            if (userProfile == null)
            {
                return new BaseResponse(true, "驗證資料有誤，請檢查輸入欄位是否正確。");
            }

            EmailSentCache cacheKey = new EmailSentCache(userProfile.EMail);
            //var resentCahceKey = _cacheFactory.GetEmailSentCache(email);
            EmailSent resentCahce = _cacheStore.Get(cacheKey);
            if (resentCahce != null)
            {
                return new BaseResponse(true, $"通知信已寄發，請查看電子信箱，或三分鐘後重新申請。");
            }

            EmailContentBodyDto emailContentBodyDto =
                new EmailContentBodyDto(contract: null, initiatorOrg: null, userProfile: userProfile);

            _emailFactory.SendEmailToCustomer(
                _emailFactory.GetApplyPassword(dto: emailContentBodyDto));


            _cacheStore.Add(new EmailSent(), cacheKey);

            return new BaseResponse(false, "");

        }

    }
}
