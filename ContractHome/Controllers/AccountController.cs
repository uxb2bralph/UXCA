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
    private readonly ICacheStore _cacheStore;
        private ContractServices? _contractServices;

        public AccountController(ILogger<HomeController> logger, 
            IServiceProvider serviceProvider, 
            ICacheStore cacheStore,
            EmailFactory emailContentFactories,
            ContractServices contractServices
            ) : base(serviceProvider)
    {
          _logger = logger;
          _cacheStore = cacheStore;
        _emailFactory = emailContentFactories;
            _contractServices = contractServices;
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
        if (userprofile.CanCreateContract())
        {
            _emailFactory.SendEmailToCustomer(
                _emailFactory.GetLoginFailed(emailUserName: userprofile.UserName, email: userprofile.EMail));
        }

        return Json(new { result = false, message = ModelState.ErrorMessage() });
        }

        if (userprofile.CanCreateContract())
        {
            _emailFactory.SendEmailToCustomer(
                _emailFactory.GetLoginSuccessed(emailUserName: userprofile.UserName, email: userprofile.EMail));
        }

        var dateNeedToUpdatePassword = DateTime.Now.AddMonths(-3);
        var res = DateTime.Compare(userprofile.PasswordUpdatedDate?? dateNeedToUpdatePassword, dateNeedToUpdatePassword);
        if (res<=0 && !userprofile.IsSysAdmin())
        {
            return Json(new { result = true, message = Url.Action("PasswordChangeView", "UserProfile") });
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
            FileLogger.Logger.Info($"{this.GetType().Name}-Trust-Token={token}");
            var trustPasswordApplyTokenCahceKey = new TrustPasswordApplyTokenCahceKey(token);
            var result = _cacheStore.Get(trustPasswordApplyTokenCahceKey);

            if (result != null)
            {
                TempData["message"] += $"Token已失效，請重新申請。";
            }

            var tokenBase64UrlDecode = JwtTokenValidator.Base64UrlDecodeToString(token);
            _contractServices.SetModels(models);
            (BaseResponse resp, JwtToken jwtTokenObj, UserProfile userProfile) = 
                _contractServices.TokenValidate(tokenBase64UrlDecode.DecryptData());

              if (resp.HasError)
              {
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
            FileLogger.Logger.Info($"{this.GetType().Name}-PasswordReset-Base64UrlEncodeToken={token}");
            var password = viewModel.Password.GetEfficientString();
      var pid = viewModel.PID.GetEfficientString();
            var trustPasswordApplyTokenCahceKey = new TrustPasswordApplyTokenCahceKey(token);
            var result = _cacheStore.Get(trustPasswordApplyTokenCahceKey);
            if (result != null)
            {
                return new BaseResponse(true, $"Token已失效，請重新申請。");
            }
            
            _contractServices.SetModels(models);
            (BaseResponse resp, JwtToken jwtTokenObj, UserProfile tokenUserProfile) 
                = _contractServices.TokenValidate(JwtTokenValidator.Base64UrlDecodeToString(token).DecryptData());
      if (resp.HasError) { return resp; }


            var isPasswordValid = Regex.IsMatch(password, UserProfileFactory.PasswordRegex);
            if (!isPasswordValid) 
            {
                return new BaseResponse(true, $"新密碼不符合格式");
            }

            var viewModelUserProfile
          = models.GetTable<UserProfile>()
              .Where(x => x.EMail.Equals(jwtTokenObj.payloadObj.data.Email))
              .Where(x=>x.PID.Equals(pid))
              .FirstOrDefault();

      if (viewModelUserProfile == null)
      {
        return new BaseResponse(true, "驗證資料有誤。");
      }

      tokenUserProfile.Password = null;
        tokenUserProfile.LoginFailedCount = 0;
        tokenUserProfile.Password2 = password.HashPassword();
        tokenUserProfile.PasswordUpdatedDate = DateTime.Now; 

      models.SubmitChanges();

            var usedTokenCahceKey = new TrustPasswordApplyTokenCahceKey(token);
            _cacheStore.Add(new Default() { ID = viewModelUserProfile.UID.ToString() }, usedTokenCahceKey);

            _emailFactory.SendEmailToCustomer(
                _emailFactory.GetPasswordUpdated(emailUserName: tokenUserProfile.UserName, email: tokenUserProfile.EMail));

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

      var email = viewModel.Email.GetEfficientString();
      if (string.IsNullOrEmpty(email))
      {
        return new BaseResponse(true, "驗證資料有誤，請檢查輸入欄位是否正確。");
      }

      UserProfile userProfile
          = models.GetTable<UserProfile>()
                .Where(x => x.EMail.Equals(email))
                .Where(x => x.PID.Equals(viewModel.PID))
                .FirstOrDefault();

      if (userProfile == null)
      {
        return new BaseResponse(true, "驗證資料有誤，請檢查輸入欄位是否正確。");
      }

        //wait to do..如果email控管重覆?
        var redoLimitCahceKey = new RedoLimitCahceKey(email);
        var uid = _cacheStore.Get(redoLimitCahceKey);
        if (uid!=null)
        {
            return new BaseResponse(true, $"通知信已寄發，請查看電子信箱，或三分鐘後重新申請。");
        }

            EmailContentBodyDto emailContentBodyDto =
                new EmailContentBodyDto(contract: null, initiatorOrg: null, userProfile: userProfile);

            _emailFactory.SendEmailToCustomer(
            _emailFactory.GetApplyPassword(dto: emailContentBodyDto));

        //wait to do:新token產生後, 設定舊token為失效
        _cacheStore.Add(new Default() { ID = userProfile.UID.ToString() }, redoLimitCahceKey);

      return new BaseResponse(false, "");

    }

        //private void SetValueToCache(string cacheItem, string cacheValue, int expirateionMin = 5, int slidingExpirateionMin = 5)
        //{
        //  var cacheExpiryOptions = new MemoryCacheEntryOptions
        //  {
        //    AbsoluteExpiration = DateTime.Now.AddMinutes(expirateionMin),
        //    //SlidingExpiration = TimeSpan.FromMinutes(slidingExpirateionMin),
        //    Priority = CacheItemPriority.Low
        //  };
        //  _memCache.Set(cacheItem, cacheValue, cacheExpiryOptions);
        //}

        //[AllowAnonymous]
        //[HttpGet]
        //public async Task<IActionResult> SignatureTrust(string token)
        //{
        //    (BaseResponse resp, JwtToken jwtTokenObj, UserProfile userProfile)
        //            = _contractServices.TokenValidate(token);
        //    if (resp.HasError)
        //    {
        //        return View(resp);
        //    }
        //    //wait to do:Trust進來可能沒有正常user權限,
        //    //但因為controller都有用var profile = await HttpContext.GetUserAsync();, 暫時先用
        //    HttpContext.SignOnAsync(userProfile);

        //    return RedirectToAction("AffixPdfSealForTrust", "ContractConsole"
        //        , new
        //        {
        //            KeyID = Int32.Parse(jwtTokenObj.payloadObj.contractId).EncryptKey(),
        //            UID = jwtTokenObj.payloadObj.id
        //        });
        //}

    }


}