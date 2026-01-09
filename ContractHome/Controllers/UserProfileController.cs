using CommonLib.Core.Utility;
using CommonLib.Utility;
using ContractHome.Helper;
using ContractHome.Helper.DataQuery;
using ContractHome.Helper.Security.MembershipManagement;
using ContractHome.Models.DataEntity;
using ContractHome.Models.Dto;
using ContractHome.Models.Email.Template;
using ContractHome.Models.Helper;
using ContractHome.Models.ViewModel;
using ContractHome.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data.Linq;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq.Dynamic.Core;
using System.Text.RegularExpressions;
using static ContractHome.Helper.JwtTokenGenerator;
using static ContractHome.Models.Helper.ContractServices;

namespace ContractHome.Controllers
{
    public class UserProfileController : SampleController
    {
        private readonly ILogger<UserProfileController> _logger;
        private readonly EmailFactory _emailFactory;
        private readonly BaseResponse _baseResponse;
        private readonly ContractServices _contractServices;
        private readonly DCDataContext _db;
        public UserProfileController(ILogger<UserProfileController> logger, 
                        IServiceProvider serviceProvider,
                        EmailFactory emailContentFactories,
                        ContractServices contractServices,
                        BaseResponse baseResponse, 
                        DCDataContext db) : base(serviceProvider)
        {
            _logger = logger;
            _emailFactory = emailContentFactories;
            _baseResponse = baseResponse;
            _contractServices = contractServices;
            _db = db;
        }

        [RoleAuthorize(roleID: new int[] { (int)UserRoleDefinition.RoleEnum.SystemAdmin, (int)UserRoleDefinition.RoleEnum.MemberAdmin })]
        public ActionResult MaintainData(QueryViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;
            return View("~/Views/UserProfile/MaintainData.cshtml");
        }

        [RoleAuthorize(roleID: new int[] { (int)UserRoleDefinition.RoleEnum.SystemAdmin, (int)UserRoleDefinition.RoleEnum.MemberAdmin })]
        public ActionResult DeptMgmt(QueryViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;
            return View("~/Views/UserProfile/DeptMgmt.cshtml");
        }

        [RoleAuthorize(roleID: new int[] { (int)UserRoleDefinition.RoleEnum.SystemAdmin, (int)UserRoleDefinition.RoleEnum.MemberAdmin })]
        public async Task<ActionResult> VueInquireDataAsync([FromBody] UserProfileViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;

            var profile = await HttpContext.GetUserAsync();

            IQueryable<UserProfile> items = models.GetTable<UserProfile>();

            if (profile?.IsMemberAdmin() == true)
            {
                var orgUser = models.GetTable<OrganizationUser>()
                                .Where(o => o.UID == profile.UID).FirstOrDefault();
                if (orgUser != null)
                {
                    var orgUsers = models.GetTable<OrganizationUser>().Where(o => o.CompanyID == orgUser.CompanyID);
                    items = items.Where(u => orgUsers.Any(o => o.UID == u.UID));
                }
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

            if (viewModel.IsEnabled.HasValue)
            {
                items = items.Where(o => o.IsEnabled == viewModel.IsEnabled.Value);
            }

            if (viewModel.CompanyName != null)
            {
                items = from u in items
                        join ou in models.GetTable<OrganizationUser>() on u.UID equals ou.UID
                        join o in models.GetTable<Organization>() on ou.CompanyID equals o.CompanyID
                        where o.CompanyName.Contains(viewModel.CompanyName)
                        select u;
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

        [RoleAuthorize(roleID: [(int)UserRoleDefinition.RoleEnum.SystemAdmin, (int)UserRoleDefinition.RoleEnum.MemberAdmin, (int)UserRoleDefinition.RoleEnum.User])]
        public async Task<ActionResult> PasswordChangeView(
            UserPasswordChangeViewModel userPasswordChange)
        {
            return View("~/Views/UserProfile/VueModule/PasswordChange.cshtml");
        }

        [RoleAuthorize(roleID: [(int)UserRoleDefinition.RoleEnum.SystemAdmin, (int)UserRoleDefinition.RoleEnum.MemberAdmin, (int)UserRoleDefinition.RoleEnum.User])]
        public async Task<ActionResult> ContractPasswordChangeView(string token)
        {
            _contractServices.SetModels(models);
            (BaseResponse resp, JwtToken jwtTokenObj, UserProfile userProfile)
                    = _contractServices.TokenValidate(JwtTokenValidator.Base64UrlDecodeToString(token).DecryptData());
            if (resp.HasError)
            {
                //return View("SignatureTrust",resp);
                throw new ArgumentException(resp.Message);
            }

            if (string.IsNullOrEmpty(jwtTokenObj.ContractID))
            {
                throw new ArgumentException("contractID is null.");
            }

            if (userProfile.PasswordUpdatedDate != null)
            {
                return RedirectToAction("Trust", "ContractConsole", new { token });
            }


            ViewBag.UserProfile = userProfile;
            ViewBag.Token = token;

            return View("~/Views/UserProfile/VueModule/ContractPasswordChange.cshtml");
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

            if (PID.Equals("ifsadmin"))
            {
                ModelState.AddModelError("PID", "變更失敗");
            }

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
                    _emailFactory.GetPasswordUpdated(emailUserName: userProfile.PID, email: userProfile.EMail));
            }
            catch (Exception ex)
            {
                FileLogger.Logger.Error(ex);
                return Ok(new { result = false });
            }

            return Ok(new { result = true });
        }

        [RoleAuthorize(roleID: new int[] { (int)UserRoleDefinition.RoleEnum.SystemAdmin, (int)UserRoleDefinition.RoleEnum.MemberAdmin })]
        public async Task<ActionResult> VueCommitItemAsync([FromBody] UserProfileViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;

            var profile = await HttpContext.GetUserAsync();

            int? uid = null;
            if (!string.IsNullOrEmpty(viewModel.KeyID))
            {
                uid = viewModel.DecryptKeyValue();
            }

            var dataItem = models.GetTable<UserProfile>()
                .Where(u => u.UID == uid)
                .FirstOrDefault();

            // 公司管理員 新增帳號 強制指定管理員的公司ID
            int? companyID = (profile.IsMemberAdmin()) ? profile.UserCompanyID : viewModel.GetCompanyID();
            // 新增帳號 檢查密碼
            if (dataItem == null && String.IsNullOrEmpty(viewModel.Password))
            {
                ModelState.AddModelError("Password", "請設定密碼");
            }

            var pidUser = models.GetTable<UserProfile>()
                .Where(u => u.PID == viewModel.PID)
                .FirstOrDefault();

            // 新增帳號 檢查PID
            if (dataItem == null && ContractServices.IsNotNull(pidUser))
            {
                ModelState.AddModelError("PID", "帳號已存在");
            }
            // 編輯帳號 排除自己UID外的帳號
            if (dataItem != null && ContractServices.IsNotNull(pidUser) && pidUser.UID != dataItem.UID)
            {
                ModelState.AddModelError("PID", "帳號已存在");
            }

            Organization? organization = null;

            if (!companyID.HasValue)
            {
                ModelState.AddModelError("EncCompanyID", "請選擇隸屬公司");
            } else
            {
                organization = models.GetTable<Organization>()
                                            .Where(o => o.CompanyID == companyID.Value)
                                            .FirstOrDefault();

                if (organization == null)
                {
                    ModelState.AddModelError("EncCompanyID", "公司資料錯誤");
                }
            }

            if (!viewModel.RoleID.HasValue)
            {
                // 公司管理員 新增帳號 強制設定為一般使用者
                if (profile.IsMemberAdmin())
                {
                    viewModel.RoleID = UserRoleDefinition.RoleEnum.User;
                }
                else
                {
                    ModelState.AddModelError("RoleID", "請選擇角色");
                }
            }

            //  預設工商憑證
            if (string.IsNullOrEmpty(viewModel.Region))
            {
                viewModel.Region = "O";
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

            int mailCount = 0;
            if (ContractServices.IsNotNull(pidUser))
            {
                mailCount = models.GetTable<UserProfile>()
                                .Where(u => u.EMail == viewModel.EMail && u.UID != pidUser.UID)
                                .Count();
            } else
            {
                mailCount = models.GetTable<UserProfile>()
                .Where(u => u.EMail == viewModel.EMail)
                .Count();
            }

            // 新增帳號 無任何人使用此mail
            if (dataItem == null && mailCount > 0)
            {
                ModelState.AddModelError("EMail", "EMail已被其他帳號使用");
            }
            // 編輯帳號 除了自己有其他相同mail
            if (dataItem != null && mailCount >= 1)
            {
                ModelState.AddModelError("EMail", "EMail已被其他帳號使用");
            }


            if (!ModelState.IsValid)
            {
                return Json(new { result = false, message = ModelState.ErrorMessage() });
            }

            UserProfile item = dataItem ?? UserProfile.PrepareNewItem(models.DataContext);

            item.PID = viewModel.PID;
            item.EMail = viewModel.EMail;
            item.UserName = viewModel.UserName.GetEfficientString();
            item.Region = organization?.CHT_Token != null ? "E" : viewModel.Region.GetEfficientString();
            item.IsEnabled = true;

            if (!String.IsNullOrEmpty(viewModel.Password))
            {
                item.Password = null;
                item.Password2 = viewModel.Password.HashPassword();
                item.LoginFailedCount = 0;
                item.PasswordUpdatedDate = DateTime.Now;
            }

            models.SubmitChanges();

            // 新增帳號 新增公司
            if (dataItem == null && companyID.HasValue)
            {
                OrganizationUser orgUser = new()
                {
                    UID = item.UID,
                    CompanyID = companyID.Value
                };
                models.GetTable<OrganizationUser>().InsertOnSubmit(orgUser);
                models.SubmitChanges();
            }

                        // 編輯帳號 換公司
            if (dataItem != null && companyID.HasValue) {
                item.OrganizationUser.CompanyID = companyID.Value;
                models.SubmitChanges();
            }

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
        public async Task<ActionResult> VueDeleteItemAsync([FromBody] UserProfileViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;

            var profile = await HttpContext.GetUserAsync();

            if (viewModel.KeyID != null)
            {
                viewModel.UID = viewModel.DecryptKeyValue();
            }

            var item = models.GetTable<UserProfile>().Where(o => o.UID == viewModel.UID)
                .FirstOrDefault();

            ITable dataTable = models.GetTable<UserProfile>();

            if (item != null)
            {
                if (profile.IsMemberAdmin() && item.OrganizationUser.CompanyID != profile.UserCompanyID)
                {
                    return Json(new { result = false, message = "無刪除權限" });
                }

                dataTable.DeleteOnSubmit(item);
                try
                {
                    models.SubmitChanges();
                    return Json(new { result = true });
                }
                catch (SqlException ex)
                {
                    FileLogger.Logger.Error(ex);
                    string message = ex.Message;
                    if (ex.Number == 547)
                    {
                        message = "刪除失敗：此帳號已有簽署記錄";
                    }

                    return Json(new { result = false, message = message });
                }
            }

            return Json(new { result = false, message = "資料錯誤！" });
        }

        [RoleAuthorize(roleID: new int[] { (int)UserRoleDefinition.RoleEnum.SystemAdmin, (int)UserRoleDefinition.RoleEnum.MemberAdmin })]
        public async Task<ActionResult> SetEnabled([FromBody] UserProfileViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;

            if (viewModel.KeyID != null)
            {
                viewModel.UID = viewModel.DecryptKeyValue();
            }

            var item = models.GetTable<UserProfile>().Where(o => o.UID == viewModel.UID)
                .FirstOrDefault();

            if (item != null)
            {
                try
                {
                    var profile = await HttpContext.GetUserAsync();

                    if (profile.IsSysAdmin)
                    {
                        item.IsEnabled = viewModel.IsEnabled.Value;
                        models.SubmitChanges();
                        return Json(new { result = true, message = "已成功" + ((viewModel.IsEnabled.Value) ? "啟用" : "停用") });
                    }

                    // 檢查使用者是否在進行中的合約做簽署
                    var result = from csr in _db.ContractSignatureRequest
                                 join c in _db.Contract on csr.ContractID equals c.ContractID
                                 join d in _db.CDS_Document on c.ContractID equals d.DocID
                                 where csr.SignerID == item.UID
                                 && d.CurrentStep != (int)CDS_Document.StepEnum.Terminated 
                                 && d.CurrentStep != (int)CDS_Document.StepEnum.Committed
                                 && d.CurrentStep != (int)CDS_Document.StepEnum.Revoked
                                 select csr;

                    if (result.Any() && viewModel.IsEnabled == false)
                    {
                        return Json(new { result = false, message = "合約簽署中" });
                    }

                    item.IsEnabled = viewModel.IsEnabled.Value;
                    models.SubmitChanges();
                    return Json(new { result = true, message = "已成功" + ((viewModel.IsEnabled.Value) ? "啟用" : "停用") });
                }
                catch (SqlException ex)
                {
                    FileLogger.Logger.Error(ex);
                    string message = ex.Message;
                    return Json(new { result = false, message = message });
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
