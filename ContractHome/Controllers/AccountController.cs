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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Caching.Memory;
using static ContractHome.Helper.JwtTokenGenerator;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Drawing;
using Newtonsoft.Json;
using CommonLib.Core.Utility;
using ContractHome.Models.Dto;
using ContractHome.Models.Cache;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ContractHome.Models.Helper;

namespace ContractHome.Controllers
{
  public class AccountController : SampleController
  {
    private readonly ILogger<HomeController> _logger;
    private readonly IMailService _mailService;
    private readonly EmailBody _emailBody;
    private readonly EmailFactory _emailFactory;
    private readonly ICacheStore _cacheStore;
        private ContractServices? _contractServices;
        //private static readonly int tokenTTLMins = 10;
        //private static readonly int reSendEmailMins = 3;
        public AccountController(ILogger<HomeController> logger, IServiceProvider serviceProvider, 
            ICacheStore cacheStore, 
            ContractServices contractServices) : base(serviceProvider)
    {
      _logger = logger;
      _mailService = ServiceProvider.GetRequiredService<IMailService>();
      _emailFactory = serviceProvider.GetRequiredService<EmailFactory>();
      _emailBody = serviceProvider.GetRequiredService<EmailBody>();
      _cacheStore = cacheStore;
      _contractServices = contractServices;
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

      if (!ModelState.IsValid)
      {
        //wait to do...甲方的公司UserEmail登入失敗通知信
        if (userprofile.CanCreateContract())
        {
          var emailBody =
              new EmailBodyBuilder(_emailBody)
              .SetTemplateItem(EmailBody.EmailTemplate.LoginFailed)
              .SetUserName(userprofile.UserName)
              .SetUserEmail(userprofile.EMail)
          .Build();

          _mailService?.SendMailAsync(await _emailFactory.GetEmailToCustomer(emailBody), default);
        }

        return Json(new { result = false, message = ModelState.ErrorMessage() });
      }

      if (userprofile.CanCreateContract())
      {
        var emailBody =
            new EmailBodyBuilder(_emailBody)
            .SetTemplateItem(EmailBody.EmailTemplate.LoginSuccessed)
            .SetUserName(userprofile.UserName)
            .SetUserEmail(userprofile.EMail)
            .Build();

        _mailService?.SendMailAsync(await _emailFactory.GetEmailToCustomer(emailBody), default);
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
    public ActionResult TrustPasswordReset([FromQuery] string token)
    {
            token = token.GetEfficientString();
            token = JwtTokenValidator.Base64UrlDecodeToString(token);

            var result = _cacheStore.Get(new TrustPasswordApplyTokenCahceKey(token));
            if (result != null)
            {
                throw new InvalidOperationException($"Token已失效，請重新申請。");
            }

            _contractServices.SetModels(models);
            (BaseResponse resp, JwtToken jwtTokenObj, UserProfile userProfile) = _contractServices.TokenValidate(token);
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
      var password = viewModel.Password.GetEfficientString();
      var pid = viewModel.PID.GetEfficientString();

            var result = _cacheStore.Get(new TrustPasswordApplyTokenCahceKey(token));
            if (result != null)
            {
                return new BaseResponse(true, $"Token已失效，請重新申請。");
            }

            _contractServices.SetModels(models);
            (BaseResponse resp, JwtToken jwtTokenObj, UserProfile tokenUserProfile) = _contractServices.TokenValidate(token);
      if (resp.HasError) { return resp; }

      var viewModelUserProfile
          = models.GetTable<UserProfile>()
              .Where(x => x.EMail.Equals(jwtTokenObj.payloadObj.email))
              .Where(x => x.PID.Equals(pid))
              .FirstOrDefault();

      if (viewModelUserProfile == null)
      {
        return new BaseResponse(true, "驗證資料有誤。");
      }

      if (!viewModelUserProfile.UID.Equals(viewModelUserProfile.UID))
      {
        return new BaseResponse(true, "驗證資料有誤。");
      }

      //wait to do...//[RegularExpression(@"^(?=.*\d)(?=.*[a-zA-Z])(?=.*\W).{8,30}$",ErrorMessage = "新密碼格式有誤，請確認")]
      tokenUserProfile.Password = null;
      tokenUserProfile.Password2 = password.HashPassword();

      models.SubmitChanges();

            var usedTokenCahceKey = new TrustPasswordApplyTokenCahceKey($"{token}");
            _cacheStore.Add(new Default(), usedTokenCahceKey);

            var emailBody =
          new EmailBodyBuilder(_emailBody)
          .SetTemplateItem(EmailBody.EmailTemplate.PasswordUpdated)
          .SetUserName(tokenUserProfile.UserName)
          .SetUserEmail(tokenUserProfile.EMail)
      .Build();

      _mailService?.SendMailAsync(await _emailFactory.GetEmailToCustomer(emailBody), default);

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
          = models.GetTable<UserProfile>().Where(x => x.EMail.Equals(email)).FirstOrDefault();
      if (userProfile == null)
      {
        return new BaseResponse(true, "驗證資料有誤，請檢查輸入欄位是否正確。");
      }

        //wait to do..如果email控管重覆?
        var redoLimitCahceKey = new RedoLimitCahceKey(email);
        var uid = _cacheStore.Get(redoLimitCahceKey);
        if (uid!=null)
        {
            return new BaseResponse(true, $"通知信已寄發，請查看電子信箱，或稍後重新申請。");
        }

            JwtPayloadData jwtPayloadData = new JwtPayloadData() { ContractID=string.Empty, Email=email, UID= userProfile.UID };
            var jwtToken = JwtTokenGenerator.GenerateJwtToken(jwtPayloadData);
            var clickLink = $"{HttpContext.DefaultWebUri()}/Account/TrustPasswordReset?token={JwtTokenGenerator.Base64UrlEncode(jwtToken)}";

            FileLogger.Logger.Error($"clickLink={clickLink}");
            var emailTemp = EmailBody.EmailTemplate.WelcomeUser;
      if (viewModel.Item!=null&&viewModel.Item.Equals("forgetPassword")) { emailTemp = EmailBody.EmailTemplate.ApplyPassword; }

      var emailBody =
          new EmailBodyBuilder(_emailBody)
          .SetTemplateItem(emailTemp)
          .SetUserName(userProfile.UserName)
          .SetUserEmail(userProfile.EMail)
          .SetVerifyLink(clickLink)
          .Build();

      var emailData = await _emailFactory.GetEmailToCustomer(emailBody);

      _mailService?.SendMailAsync(emailData, default);

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