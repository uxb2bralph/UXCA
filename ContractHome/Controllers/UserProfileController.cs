using Microsoft.AspNetCore.Mvc;
using ContractHome.Models.DataEntity;
using ContractHome.Models.ViewModel;
using CommonLib.Utility;
using Newtonsoft.Json;
using ContractHome.Helper;
using CommonLib.Core.Utility;
using System.Drawing;
using System.Linq.Dynamic.Core;
using System.Data.Linq;
using ContractHome.Security.Authorization;
using ContractHome.Helper.Security.MembershipManagement;
using ContractHome.Models.Dto;
using static ContractHome.Models.Helper.ContractServices;
using ContractHome.Helper.DataQuery;
using Microsoft.AspNetCore.Authorization;
using System.Text.RegularExpressions;
using ContractHome.Models.Email.Template;
using ContractHome.Models.Helper;

namespace ContractHome.Controllers
{
    public class UserProfileController : SampleController
    {
        private readonly ILogger<UserProfileController> _logger;
        private readonly EmailFactory _emailFactory;
        private readonly BaseResponse _baseResponse;
        public UserProfileController(ILogger<UserProfileController> logger, 
                        IServiceProvider serviceProvider,
                        EmailFactory emailContentFactories,
                        BaseResponse baseResponse) : base(serviceProvider)
        {
            _logger = logger;
            _emailFactory = emailContentFactories;
            _baseResponse = baseResponse;   
        }

        [RoleAuthorize(roleID: new int[] { (int)UserRoleDefinition.RoleEnum.SystemAdmin, (int)UserRoleDefinition.RoleEnum.MemberAdmin })]
        public ActionResult MaintainData(QueryViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;
            return View("~/Views/UserProfile/MaintainData.cshtml");
        }

        [Authorize]
        public async Task<ActionResult> VueInquireDataAsync([FromBody] UserProfileViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;

            var profile = await HttpContext.GetUserAsync();

            IQueryable<UserProfile> items = models.GetTable<UserProfile>();
            if (profile?.IsSysAdmin() == true)
            {

            }
            else if (profile?.IsMemberAdmin() == true)
            {
                var orgUser = models.GetTable<OrganizationUser>()
                                .Where(o => o.UID == profile.UID).FirstOrDefault();
                if (orgUser != null)
                {
                    var orgUsers = models.GetTable<OrganizationUser>().Where(o => o.CompanyID == orgUser.CompanyID);
                    items = items.Where(u => orgUsers.Any(o => o.UID == u.UID));
                }
                else
                {
                    items = items.Where(p => false);
                }
            }
            else
            {
                items = items.Where(p => false);
            }

            int? companyID = viewModel.GetCompanyID();
            if (companyID.HasValue)
            {
                var orgUsers = models.GetTable<OrganizationUser>()
                                .Where(c => c.CompanyID == companyID);
                items = items.Where(u => orgUsers.Any(c => c.UID == u.UID));
            }

            viewModel.PID = viewModel.PID.GetEfficientString();
            if (viewModel.PID != null)
            {
                items = items.Where(o => o.PID.StartsWith(viewModel.PID));
            }

            viewModel.UserName = viewModel.UserName.GetEfficientString();
            if (viewModel.UserName != null)
            {
                items = items.Where(o => o.UserName.StartsWith(viewModel.UserName));
            }

            viewModel.EMail = viewModel.EMail.GetEfficientString();
            if (viewModel.EMail != null)
            {
                items = items.Where(o => o.EMail.StartsWith(viewModel.EMail));
            }


            //if (viewModel.DataItem != null && viewModel.DataItem.Length > 0)
            //{
            //    items = items.BuildQuery(viewModel.DataItem);
            //}

            if (viewModel.PageIndex.HasValue)
            {
                viewModel.PageIndex--;
            }
            else
            {
                viewModel.PageIndex = 0;
            }

            return View("~/Views/UserProfile/VueModule/UserProfileList.cshtml", items);
        }

        [RoleAuthorize(roleID: new int[] { (int)UserRoleDefinition.RoleEnum.SystemAdmin, (int)UserRoleDefinition.RoleEnum.MemberAdmin })]
        public ActionResult InquireData(UserProfileViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;

            IQueryable<UserProfile> items = models.GetTable<UserProfile>();

            if (viewModel.DataItem != null && viewModel.DataItem.Length > 0)
            {
                items = items.BuildQuery(viewModel.DataItem);
            }

            int? companyID = viewModel.GetCompanyID();
            if (companyID.HasValue)
            {
                var orgUsers = models.GetTable<OrganizationUser>()
                                .Where(c => c.CompanyID == companyID);
                items = items.Where(u => orgUsers.Any(c => c.UID == u.UID));
            }

            var item = items.FirstOrDefault();
            return View("~/Views/UserProfile/Module/EditItem.cshtml", item);
        }

        [RoleAuthorize(roleID: new int[] { (int)UserRoleDefinition.RoleEnum.SystemAdmin, (int)UserRoleDefinition.RoleEnum.MemberAdmin })]
        public async Task<ActionResult> DataItemAsync(QueryViewModel viewModel)
        {
            if (ViewBag.ViewModel == null)
            {
                viewModel = await PrepareViewModelAsync(viewModel);
            }

            viewModel.EncKeyItem = viewModel.EncKeyItem.GetEfficientString();
            if (viewModel.EncKeyItem != null)
            {
                viewModel.KeyItem = JsonConvert.DeserializeObject<DataTableColumn[]>(viewModel.EncKeyItem.DecryptData());
            }

            IQueryable<UserProfile> items = models.GetTable<UserProfile>();

            if (viewModel.KeyItem?.Any(k => k.Name != null && k.Value != null) == true)
            {
                items = items.BuildQuery(viewModel.KeyItem);
            }
            else
            {
                items = items.Where(" 1 = 0");
            }

            var dataItem = items.FirstOrDefault();
            return View("~/Views/UserProfile/Module/EditItem.cshtml", dataItem);
        }

        [UserAuthorize]
        public async Task<ActionResult> PasswordChangeView(
            UserPasswordChangeViewModel userPasswordChange)
        {
            return View("~/Views/UserProfile/VueModule/PasswordChange.cshtml");
        }

        //[RoleAuthorize(roleID: new int[] {
        //    (int)UserRoleDefinition.RoleEnum.User,
        //    (int)UserRoleDefinition.RoleEnum.MemberAdmin })]
        [UserAuthorize]
        [HttpPost]
        //[RoleAuthorize(roleID: new int[] {(int)UserRoleDefinition.RoleEnum.User,(int)UserRoleDefinition.RoleEnum.MemberAdmin })]
        public async Task<ActionResult> PasswordChange(
      [FromBody] UserPasswordChangeViewModel userPasswordChange)
        {

            if (string.IsNullOrEmpty(userPasswordChange.EncPID))
            {
                ModelState.AddModelError("PID", "認證失敗");
                return Json(new { result = false, message = ModelState.ErrorMessage() });
            }

            var isPasswordValid = Regex.IsMatch(userPasswordChange.NewPassword, UserProfileFactory.PasswordRegex);
            if (!isPasswordValid)
            {
                ModelState.AddModelError("NewPassword", "新密碼不符合格式");
                return Json(new { result = false, message = ModelState.ErrorMessage() });
            }

            var PID = userPasswordChange.PID.DecryptData();
            UserProfile profile = UserProfileFactory.LoginProfileCheck(
                  pid: PID,
                  password: userPasswordChange.OldPassword,
                  out int? loginFailedCount);

            if (loginFailedCount >= 3)
            {
                ModelState.AddModelError("PID", "帳號密碼有誤");
                return Json(new { result = false, message = ModelState.ErrorMessage() });
            }

            if (profile == null)
            {
                ModelState.AddModelError("PID", "認證失敗");
                return Json(new { result = false, message = ModelState.ErrorMessage() });
            }

            var result = UserProfileFactory.CompareEncryptedPassword(
                  userPasswordChange.NewPassword,
                  profile.Password2);
            if (result)
            {
                ModelState.AddModelError("NewPassword", "新密碼與舊密碼相同");
            }

            if (!ModelState.IsValid)
            {
                return Json(new { result = false, message = ModelState.ErrorMessage() });
            }

            try
            {
                UserProfile userProfile
                    = models.GetTable<UserProfile>()
                        .Where(x => x.UID.Equals(profile.UID))
                        .FirstOrDefault();

                userProfile.LoginFailedCount = 0;
                userProfile.Password = null;
                userProfile.Password2 = userPasswordChange.NewPassword.HashPassword();
                userProfile.PasswordUpdatedDate = DateTime.Now;

                models.SubmitChanges();
                //wait to do...和Account/PasswordReset的密碼更新作業合併
                //wait to do...和UserProfile/VueCommitItem的新增合併
                _emailFactory.SendEmailToCustomer(
                    _emailFactory.GetPasswordUpdated(emailUserName: userProfile.UserName, email: userProfile.EMail));
            }
            catch (Exception ex)
            {
                FileLogger.Logger.Error(ex);
                return Ok(new { result = false });
            }

            return Ok(new { result = true });
        }

        [RoleAuthorize(roleID: new int[] { (int)UserRoleDefinition.RoleEnum.SystemAdmin, (int)UserRoleDefinition.RoleEnum.MemberAdmin })]
        public ActionResult VueCommitItem([FromBody] UserProfileViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;

            int? uid = null;
            if (viewModel.KeyID != null)
            {
                uid = viewModel.DecryptKeyValue();
            }

            var dataItem = models.GetTable<UserProfile>()
                .Where(u => u.UID == uid)
                .FirstOrDefault();

            int? companyID = viewModel.GetCompanyID();
            if (dataItem == null)
            {
                if (String.IsNullOrEmpty(viewModel.Password))
                {
                    ModelState.AddModelError("Password", "請設定密碼");
                }
            }

            var pidUser = models.GetTable<UserProfile>()
                .Where(u => u.PID == viewModel.PID)
                .FirstOrDefault();

            if (ContractServices.IsNotNull(pidUser))
            {
                ModelState.AddModelError("PID", "帳號已存在");
            }

            if (!companyID.HasValue)
            {
                ModelState.AddModelError("EncCompanyID", "請選擇隸屬公司");
            }

            if (!viewModel.RoleID.HasValue)
            {
                ModelState.AddModelError("RoleID", "請選擇角色");
            }

            viewModel.PID = viewModel.PID.GetEfficientString();
            if (viewModel.PID == null)
            {
                ModelState.AddModelError("PID", "請輸入帳號");
            }

            viewModel.EMail = viewModel.EMail.GetEfficientString();
            if (viewModel.EMail == null)
            {
                ModelState.AddModelError("EMail", "請輸入EMail");
            }

            if (!ModelState.IsValid)
            {
                return Json(new { result = false, message = ModelState.ErrorMessage() });
            }

            UserProfile item = dataItem ?? UserProfile.PrepareNewItem(models.DataContext);

            item.PID = viewModel.PID;
            item.EMail = viewModel.EMail;
            item.UserName = viewModel.UserName.GetEfficientString();
            item.Region = viewModel.Region.GetEfficientString();

            if (!String.IsNullOrEmpty(viewModel.Password))
            {
                item.Password = null;
                item.Password2 = viewModel.Password.HashPassword();
                item.LoginFailedCount = 0;
                item.PasswordUpdatedDate = DateTime.Now;
            }

            models.SubmitChanges();

            models.GetTable<OrganizationUser>().InsertOnSubmit(
                new OrganizationUser() { UID = item.UID, CompanyID = companyID ?? 0 });

            models.SubmitChanges();
            if (viewModel.RoleID.HasValue)
            {
                models.ExecuteCommand(@"DELETE FROM UserRole WHERE (UID = {0})", item.UID);
                models.ExecuteCommand(@"INSERT INTO UserRole (UID, RoleID)
                                        VALUES ({0},{1})", item.UID, (int?)viewModel.RoleID);
            }

            return Json(_baseResponse);
        }

        [RoleAuthorize(roleID: new int[] { (int)UserRoleDefinition.RoleEnum.SystemAdmin, (int)UserRoleDefinition.RoleEnum.MemberAdmin })]
        public async Task<ActionResult> CommitItemAsync(UserProfileViewModel viewModel)
        {
            viewModel = await PrepareViewModelAsync(viewModel);
            ViewResult result = (ViewResult)await DataItemAsync(viewModel);
            dynamic? dataItem = result.Model;

            int? companyID = viewModel.GetCompanyID();
            if (dataItem == null)
            {
                if (!companyID.HasValue)
                {
                    ModelState.AddModelError("EncCompanyID", "請選擇隸屬公司");
                }

                if (!viewModel.RoleID.HasValue)
                {
                    ModelState.AddModelError("RoleID", "請選擇角色");
                }

                if (String.IsNullOrEmpty(viewModel.Password))
                {
                    ModelState.AddModelError("Password", "請設定密碼");
                }
            }

            viewModel.PID = viewModel.PID.GetEfficientString();
            if (viewModel.PID == null)
            {
                ModelState.AddModelError("PID", "請輸入帳號");
            }

            viewModel.EMail = viewModel.EMail.GetEfficientString();
            if (viewModel.EMail == null)
            {
                ModelState.AddModelError("EMail", "請輸入EMail");
            }

            if (!ModelState.IsValid)
            {
                return View("~/Views/Shared/ReportInputError.cshtml");
            }

            ITable dataTable = models.GetTable<UserProfile>();
            Type type = typeof(UserProfile);
            dataItem = ContractHome.Models.DataEntity.ExtensionMethods.PrepareDataItem(viewModel.DataItem, dataItem, dataTable, type);

            UserProfile item = (dataItem as UserProfile)!;
            if (companyID.HasValue)
            {
                item.OrganizationUser.CompanyID = companyID.Value;
            }

            if (!String.IsNullOrEmpty(viewModel.Password))
            {
                item.Password = null;
                item.Password2 = viewModel.Password.HashPassword();
                item.PasswordUpdatedDate = DateTime.Now;
            }

            try
            {
                models.SubmitChanges();
                if (viewModel.RoleID.HasValue)
                {
                    models.ExecuteCommand(@"DELETE FROM UserRole WHERE (UID = {0})", item.UID);
                    models.ExecuteCommand(@"INSERT INTO UserRole (UID, RoleID)
                                            VALUES ({0},{1})", item.UID, (int?)viewModel.RoleID);
                }

                return Json(new { result = true });
            }
            catch (Exception ex)
            {
                FileLogger.Logger.Error(ex);
                return Json(new { result = false, message = ex.Message });
            }
        }

        [RoleAuthorize(roleID: new int[] { (int)UserRoleDefinition.RoleEnum.SystemAdmin, (int)UserRoleDefinition.RoleEnum.MemberAdmin })]
        public async Task<ActionResult> DeleteItemAsync(QueryViewModel viewModel)
        {
            viewModel = await PrepareViewModelAsync(viewModel);
            ViewResult result = (ViewResult)await DataItemAsync(viewModel);
            dynamic? item = result.Model;

            ITable dataTable = models.GetTable<UserProfile>();
            if (item != null)
            {
                dataTable.DeleteOnSubmit(item);
                try
                {
                    models.SubmitChanges();
                    return Json(new { result = true });
                }
                catch (Exception ex)
                {
                    FileLogger.Logger.Error(ex);
                    return Json(new { result = false, message = ex.Message });
                }
            }

            return Json(new { result = false, message = "資料錯誤！" });
        }

        [RoleAuthorize(roleID: new int[] { (int)UserRoleDefinition.RoleEnum.SystemAdmin, (int)UserRoleDefinition.RoleEnum.MemberAdmin })]
        public ActionResult VueDeleteItem([FromBody] UserProfileViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;

            if (viewModel.KeyID != null)
            {
                viewModel.UID = viewModel.DecryptKeyValue();
            }

            var item = models.GetTable<UserProfile>().Where(o => o.UID == viewModel.UID)
                .FirstOrDefault();

            ITable dataTable = models.GetTable<UserProfile>();
            if (item != null)
            {
                dataTable.DeleteOnSubmit(item);
                try
                {
                    models.SubmitChanges();
                    return Json(new { result = true });
                }
                catch (Exception ex)
                {
                    FileLogger.Logger.Error(ex);
                    return Json(new { result = false, message = ex.Message });
                }
            }

            return Json(new { result = false, message = "資料錯誤！" });
        }

        [UserAuthorize]
        public async Task<ActionResult> ShowSealModalAsync([FromBody] QueryViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;

            var profile = await HttpContext.GetUserAsync();

            IQueryable<SealTemplate> items =
                models.GetTable<SealTemplate>()
                    .Where(s => !s.Disabled.HasValue || s.Disabled == false);

            if (profile != null)
            {
                items = items.Where(s => s.UID == profile.UID);
            }
            else
            {
                items = items.Where(s => false);
            }

            return View("~/Views/UserProfile/VueModule/SealModal.cshtml", items);
        }

        [UserAuthorize]
        public async Task<ActionResult> CommitSealTemplateAsync(QueryViewModel viewModel, IFormFile sealImage)
        {
            if (sealImage == null)
            {
                return Json(new { result = false, message = "請選擇印鑑章!!" });
            }

            var profile = await HttpContext.GetUserAsync();

            if (profile == null)
            {
                return Json(new { result = false, message = "請重新登入!!" });
            }

            using (MemoryStream ms = new MemoryStream())
            {
                sealImage.CopyTo(ms);
                using (Bitmap bmp = new Bitmap(ms))
                {
                    SealTemplate item = new SealTemplate
                    {
                        UID = profile.UID,
                        FilePath = sealImage.FileName,
                        SealImage = new System.Data.Linq.Binary(ms.ToArray()),
                        Width = bmp.Width,
                        Height = bmp.Height,
                    };

                    models.GetTable<SealTemplate>().InsertOnSubmit(item);
                    models.SubmitChanges();

                    if (viewModel.ResultMode == DataResultMode.DataContent)
                    {
                        return Content((new
                        {
                            result = true,
                            dataItem =
                            new
                            {
                                KeyID = item.SealID.EncryptKey(),
                                Src = $"data:application/octet-stream;base64,{Convert.ToBase64String(item.SealImage.ToArray())}",
                            }
                        }).JsonStringify(), "application/json");
                    }
                    else
                    {
                        return View("~/Views/UserProfile/VueModule/SealModalItem.cshtml", item);
                    }

                }
            }
        }

        [UserAuthorize]
        public async Task<ActionResult> CommitSignatureTemplateAsync([FromBody] UserProfileViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;
            if (viewModel.SealData == null)
            {
                return Json(new { result = false, message = "請簽名!!" });
            }

            var profile = await HttpContext.GetUserAsync();

            if (profile == null)
            {
                return Json(new { result = false, message = "請重新登入!!" });
            }

            using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(viewModel.SealData)))
            {
                using (Bitmap bmp = new Bitmap(ms))
                {
                    SealTemplate item = new SealTemplate
                    {
                        UID = profile.UID,
                        SealImage = new System.Data.Linq.Binary(ms.ToArray()),
                        Width = bmp.Width,
                        Height = bmp.Height,
                    };

                    models.GetTable<SealTemplate>().InsertOnSubmit(item);
                    models.SubmitChanges();

                    if (viewModel.ResultMode == DataResultMode.DataContent)
                    {
                        return Content((new
                        {
                            result = true,
                            dataItem =
                            new
                            {
                                KeyID = item.SealID.EncryptKey(),
                                Src = $"data:application/octet-stream;base64,{Convert.ToBase64String(item.SealImage.ToArray())}",
                            }
                        }).JsonStringify(), "application/json");
                    }
                    else
                    {
                        return View("~/Views/UserProfile/VueModule/SealModalItem.cshtml", item);
                    }

                }
            }
        }


        [UserAuthorize]
        public async Task<ActionResult> CommitToDeleteSealAsync(SealRequestViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;

            var sealID = viewModel.SealID;
            if (viewModel.KeyID != null)
            {
                sealID = viewModel.DecryptKeyValue();
            }

            var profile = await HttpContext.GetUserAsync();

            var item = models.GetTable<SealTemplate>()
                .Where(s => s.SealID == sealID)
                .Where(s => s.UID == profile.UID)
                .FirstOrDefault();

            if (item == null)
            {
                return Json(new { result = false, message = "印鑑資料錯誤!!" });
            }

            item.Disabled = true;
            models.SubmitChanges();

            return Json(new { result = true });

        }

        [Authorize]
        [HttpGet]
        [AutoValidateAntiforgeryToken]
        public async Task<ActionResult> GetUserAsync()
        {
            var profile = (await HttpContext.GetUserAsync()).LoadInstance(models);
            if (profile == null)
            {
                return Json(new BaseResponse(true, "驗證失敗"));
            }

            var isSignExchange = true;
            var companyName = string.Empty;
            if (profile.IsSysAdmin())
            {
                isSignExchange = false;
            }
            else
            {
                isSignExchange = (profile.OrganizationUser.Organization.DigitalSignBy() == DigitalSignCerts.Exchange);
                companyName = profile.OrganizationUser.Organization.CompanyName;
            }

            ClientUserInfo userResponse = new()
            {
                CompanyName = companyName,
                IsMemberAdmin = profile.IsMemberAdmin(),
                IsSysAdmin = profile.IsSysAdmin(),
                UserName = profile.PID,
                EUID = profile.UID.EncryptKey(),
                IsSignExchange = isSignExchange ? true : false
            };

            return Json(new BaseResponse() { Data = userResponse });

        }


    }
}
