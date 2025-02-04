using CommonLib.Core.Utility;
using ContractHome.Helper;
using ContractHome.Helper.DataQuery;
using ContractHome.Models.Cache;
using ContractHome.Models.DataEntity;
using ContractHome.Models.Dto;
using ContractHome.Models.Email.Template;
using ContractHome.Models.Helper;
using ContractHome.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Web;
using static ContractHome.Controllers.ContractConsoleController;
using static ContractHome.Helper.JwtTokenGenerator;
using static ContractHome.Models.DataEntity.CDS_Document;
using static ContractHome.Models.Helper.ContractServices;

namespace ContractHome.Controllers
{
    //remark for testing by postman
    //[Authorize]
    public class TaskController : SampleController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ContractServices _contractServices;
        private readonly BaseResponse _baseResponse;
        private readonly ICacheStore _cacheStore;
        private readonly EmailFactory _emailContentFactories;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TaskController(ILogger<HomeController> logger,
            IServiceProvider serviceProvider,
            ICacheStore cacheStore,
            ContractServices contractServices,
            EmailFactory emailContentFactories,
            IHttpContextAccessor httpContextAccessor,
            BaseResponse baseResponse
          ) : base(serviceProvider)
        {
            _logger = logger;
            _contractServices = contractServices;
            _cacheStore = cacheStore;
            _emailContentFactories = emailContentFactories;
            _httpContextAccessor = httpContextAccessor;
            _baseResponse = baseResponse;
        }

        public async Task<ActionResult> ShowCurrentContractAsync(SignContractViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;
            //viewModel.KeyID = viewModel.KeyID.GetEfficientString();
            if (viewModel.KeyID != null)
            {
                viewModel.ContractID = viewModel.DecryptKeyValue();
            }

            var contract = models.GetTable<Contract>()
                            .Where(c => c.ContractID == viewModel.ContractID)
                            .FirstOrDefault();

            Response.Clear();
            Response.ContentType = "application/pdf";
            Response.Headers.Add("Cache-control", "max-age=1");

            if (contract != null)
            {
                var profile = await HttpContext.GetUserAsync();

                //contract.CDS_Document.DocumentProcessLog.Add(new DocumentProcessLog
                //{
                //    LogDate = DateTime.Now,
                //    ActorID = profile!.UID,
                //    StepID = (int)CDS_Document.StepEnum.Browsed,
                //});

                //models.SubmitChanges();
                _contractServices.SetModels(models);
                _contractServices.CDS_DocumentTransitStep(contract, profile!.UID, CDS_Document.StepEnum.Browsed, true);

                if (viewModel.ResultMode == DataResultMode.Download)
                {
                    Response.Headers.Add("Content-Disposition", String.Format("attachment;filename={0}.pdf", HttpUtility.UrlEncode(contract.ContractNo)));
                }

                if (contract.CDS_Document.IsPDF)
                {
                    using (MemoryStream output = contract.TaskBuildContractWithSignature(viewModel.Preview == true))
                    {
                        await Response.Body.WriteAsync(output.ToArray());
                    }
                }
                ////gembox:暫不維護
                //else
                //{
                //    using (MemoryStream output = contract.BuildCurrentContract(models, viewModel.Preview == true))
                //    {
                //        await Response.Body.WriteAsync(output.ToArray());
                //    }
                //}
                await Response.Body.FlushAsync();
            }

            return new EmptyResult { };
        }

        public async Task<ActionResult> LoadSignatureRequestAsync(SignContractViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;
            var ttt = viewModel.KeyID.DecryptKeyValue();
            _contractServices.SetModels(models);
            Contract? contract = _contractServices.GetContractByID(ttt);

            if (ContractServices.IsNull(contract))
            {
                return Ok(_baseResponse.ErrorMessage("合約不存在"));
            }

            var profile = (await HttpContext.GetUserAsync()).LoadInstance(models);
            //if (profile?.OrganizationUser == null)
            //{
            //    return Json(_baseResponse.ErrorMessage("合約簽署人資料錯誤!!"));
            //}
            if(!contract.ContractingUser.Any(x => x.UserID == profile.UID))
            {
                return Json(_baseResponse.ErrorMessage("合約簽署人資料錯誤!!"));
            }

            #region 同文件同時間簽章檢查
            if (contract.HasUserInProgress && !contract.IsSameUserInProgress(profile.UID))
            {
                return Json(_baseResponse.ErrorMessage("其他人簽署中, 請稍後再試!!"));
            }

            contract.UserInProgress = profile.UID;
            models.SubmitChanges();
            #endregion

            var requestItem =
                models.GetTable<ContractUserSignatureRequest>()
                    .Where(p => p.ContractID == contract.ContractID)
                    .Where(o => o.UserID == profile.UID)
                    .FirstOrDefault();

            if (requestItem == null)
            {
                return Json(_baseResponse.ErrorMessage("合約未建立!!"));
            }

            ViewBag.SignatureRequest = requestItem;
            ViewBag.Contract = contract;
            ViewBag.Profile = profile;

            _baseResponse.Data = new { contract.ContractNo, contract.Title };
            return Json(_baseResponse);
        }

        public async Task<ActionResult> CommitDigitalSignatureAsync(SignatureRequestViewModel viewModel)
        {
            var result = await LoadSignatureRequestAsync(viewModel);
            Contract contract = ViewBag.Contract as Contract;
            ContractUserSignatureRequest? item = ViewBag.SignatureRequest as ContractUserSignatureRequest;

            if (item == null)
            {
                return result;
            }

            UserProfile profile = (UserProfile)ViewBag.Profile;

            if (!item.SignerID.HasValue)
            {
                DigitalSignCerts userSignCert = ContractServices.GetUserSignCert(profile);

                bool isSigned = false;
                if (userSignCert == DigitalSignCerts.Enterprise)
                {
                    isSigned = models.TaskCHT_SignPdfByEnterprise(signer:profile,request: item);
                }
                //else if (userSignCert == DigitalSignCerts.UXB2B)
                //{
                //    isSigned = models.SignPdfByLocalUser(request: null, signer: profile);
                //}
                else
                {
                    //iris:目前未使用
                    //if (Properties.Settings.Default.IsIdentityCertCheck)
                    //{
                    //    IdentityCertRepo identityCertRepo = new(models);
                    //    var identityCert = identityCertRepo.GetByUid(profile.UID).FirstOrDefault();
                    //    if (identityCert == null)
                    //    {
                    //        ModelState.AddModelError("Signature", "使用者未註冊憑證");
                    //        return BadRequest();
                    //    }

                    //    IdentityCertHelper identityCertHelper = new(x509PemString: identityCert.X509Certificate);
                    //    if (!identityCertHelper.IsSignatureValid(profile.PID, viewModel.Signature))
                    //    {
                    //        ModelState.AddModelError("Signature", "驗章失敗");
                    //        return BadRequest();
                    //    }
                    //}

                    ViewBag.DataItem = item;
                    var content = profile.CHT_UserRequestTicket();
                    //var tid = ((String)content["tid"]).GetEfficientString();
                    bool isTicketResultOK = ((string)content["result"]).Equals("1");
                    bool isTicketResultAcquireCertificate = content["result"]?.Equals("13008") ?? false;
                    string discountCode = (string)content["discountCode"]??string.Empty;
                    bool hasDiscountCode = !string.IsNullOrEmpty(discountCode);

                    var tid = ((string)content["tid"]);
                    if (isTicketResultOK && !string.IsNullOrEmpty(tid))
                    {
                        item.RequestTicket = tid;
                        models.SubmitChanges();

                        return View("~/Views/Task/PrepareCHTSigning.cshtml", content);
                    } 
                    else if (isTicketResultAcquireCertificate && hasDiscountCode)
                    {
                        content = profile.CHT_RequireIssue(discountCode);
                        if ((int?)content["result"] == 1)
                        {
                            return View("~/Views/Task/PromptToAcquireCertificate.cshtml", content);
                        }
                    }

                    return Json(_baseResponse.ErrorMessage($"錯誤代碼: {content["result"]}"));
                }
                //wait to do...CommitUserSignatureAsync if (!item.SignerID.HasValue), 動作一樣
                if (isSigned)
                {
                    item.Contract.ContractUserSignature = new ContractUserSignature
                    {
                        ContractUserSignatureRequest = item,
                    };

                    item.SignerID = profile.UID;
                    item.SignatureDate = DateTime.Now;

                    models.SubmitChanges();

                    _contractServices.SetModels(models);
                    _contractServices.CDS_DocumentTransitStep(item.Contract, profile!.UID, CDS_Document.StepEnum.DigitalSigned, true);

                    if (!models.GetTable<ContractUserSignatureRequest>()
                        .Where(c => c.ContractID == item.ContractID)
                        .Where(c => !c.SignerID.HasValue)
                        .Any())
                    {
                        _contractServices.CDS_DocumentTransitStep(item.Contract, profile!.UID, CDS_Document.StepEnum.Committed, true);

                        EmailContentBodyDto emailContentBodyDto =
                            new EmailContentBodyDto(contract: item?.Contract, initiatorOrg: null, userProfile: profile);

                        var targetUsers = _contractServices.GetUsersbyContract(item.Contract);
                        if (targetUsers != null)
                        {
                            _contractServices.SendUsersNotifyEmailAboutContractAsync(
                                item.Contract,
                                _emailContentFactories.GetFinishContract(emailContentBodyDto),
                                targetUsers);
                        }

                    }

                    if ((UserSession.Get(_httpContextAccessor) != null) && (UserSession.Get(_httpContextAccessor).IsTrust))
                    {
                        UserSession.Remove(_httpContextAccessor);
                        HttpContext.Logout();
                    }

                    if (ContractServices.IsNotNull(contract) && contract.HasUserInProgress)
                    {
                        contract.UserInProgress = null;
                        models.SubmitChanges();
                    }

                    return Json(_baseResponse);

                }
            }
            else
            {
                return Json(new BaseResponse(haserror: true, error: "合約已有簽署記錄, 無法再次簽署."));
            }

            if (viewModel.IsTrust != null && viewModel.IsTrust == true)
            {
                _baseResponse.Url = $"{ContractHome.Properties.Settings.Default.WebAppDomain}";
                return View("~/Views/Shared/CustomMessage.cshtml", _baseResponse);
            }

            return Json(_baseResponse);

        }

        public async Task<ActionResult> CommitUserSignatureAsync(SignatureRequestViewModel viewModel)
        {
            var result = await LoadSignatureRequestAsync(viewModel);

            ContractUserSignatureRequest? item = ViewBag.SignatureRequest as ContractUserSignatureRequest;

            if (item == null)
            {
                return result;
            }

            UserProfile profile = (UserProfile)ViewBag.Profile;
            //wait to do...合併CommitDigitalSignatureAsync if(Signed), 動作一樣
            if (!item.SignerID.HasValue)
            {
                (bool signOk, string code) = models.TaskCHT_SignPdfByUser(item, profile);
                if (signOk)
                {
                    item.Contract.ContractUserSignature = new ContractUserSignature
                    {
                        ContractUserSignatureRequest = item,
                    };

                    item.SignerID = profile.UID;
                    item.SignatureDate = DateTime.Now;

                    models.SubmitChanges();

                    _contractServices.SetModels(models);
                    _contractServices.CDS_DocumentTransitStep(item.Contract, profile!.UID, CDS_Document.StepEnum.DigitalSigned,true);

                    if (!models.GetTable<ContractUserSignatureRequest>()
                        .Where(c => c.ContractID == item.ContractID)
                        .Where(c => !c.SignerID.HasValue)
                        .Any())
                    {
                        _contractServices.CDS_DocumentTransitStep(item.Contract, profile!.UID, CDS_Document.StepEnum.Committed,true);

                        EmailContentBodyDto emailContentBodyDto =
                            new EmailContentBodyDto(contract: item.Contract, initiatorOrg: null, userProfile: profile);

                        var targetUsers = _contractServices.GetUsersbyContract(item.Contract);
                        _contractServices.SendUsersNotifyEmailAboutContractAsync(
                            item.Contract,
                            _emailContentFactories.GetFinishContract(emailContentBodyDto),
                            targetUsers);

                    }

                    if ((UserSession.Get(_httpContextAccessor) != null) && (UserSession.Get(_httpContextAccessor).IsTrust))
                    {
                        UserSession.Remove(_httpContextAccessor);
                        HttpContext.Logout();
                    }

                    if (ContractServices.IsNotNull(item.Contract) && item.Contract.HasUserInProgress)
                    {
                        item.Contract.UserInProgress = null;
                        models.SubmitChanges();
                    }

                    return Json(new BaseResponse());
                }
                else
                {
                    return Json(new BaseResponse(haserror: true, error: code));
                }
            }

            return Json(new BaseResponse());

        }

        //2024.05.29 iris:查詢畫面的[終止文件], 和[AbortContractAsync]結果一樣, 更新文件狀態為[CDS_Document.StepEnum.Revoked]
        public async Task<ActionResult> TerminateContractAsync(SignatureRequestViewModel viewModel)
        {
            //var result = LoadContract(viewModel);
            Contract item = _contractServices.GetContractByID(viewModel.ContractID);

            //if (item == null)
            //{
            //    return result;
            //}

            if (item.CDS_Document.CurrentStep == (int)CDS_Document.StepEnum.Terminated)
            {
                return Json(new { result = true });
            }

            _contractServices.SetModels(models);
            var profile = (await HttpContext.GetUserAsync()).LoadInstance(models);
            if (profile?.IsSysAdmin() == true)
            {
                if (!item.CDS_Document.CurrentStep.HasValue || item.CDS_Document.CurrentStep == (int)CDS_Document.StepEnum.Initial)
                {
                    models.DeleteAny<CDS_Document>(d => d.DocID == item.ContractID);
                    return Json(new { result = true });
                }
                else
                {
                    _contractServices.CDS_DocumentTransitStep(item, profile!.UID, CDS_Document.StepEnum.Revoked,true);
                    return Json(new { result = true });
                }
            }
            else
            {
                if (profile?.OrganizationUser == null)
                {
                    return Json(new { result = false, message = "合約簽署人資料錯誤!!" });
                }

                if (!models.GetTable<ContractingUser>()
                        .Where(p => p.ContractID == item.ContractID)
                        .Where(p => p.UserID == profile.UID)
                        .Any())
                {
                    return Json(new { result = false, message = "合約簽署人資料錯誤!!" });
                }

                _contractServices.CDS_DocumentTransitStep(item, profile!.UID, CDS_Document.StepEnum.Terminated,true);
                return Json(new { result = true });
            }


        }


        private IQueryable<Contract> PromptContractItems(UserProfile profile)
        {

            IQueryable<Contract> items =
                models.GetTable<Contract>();

            if (profile.IsSysAdmin())
            {

            }
            else
            {
                items = items.Where(c => models.GetTable<ContractingUser>()
                                    .Where(p => p.UserID == profile.UID)
                             .Any(p => p.ContractID == c.ContractID));
            }

            return items;
        }

        public async Task<ActionResult> InquireDataAsync([FromBody] ContractQueryViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;

            var profile = await HttpContext.GetUserAsync();
            IQueryable<Contract> items = PromptContractItems(profile);

            if (viewModel.ContractNo != null)
            {
                items = items.Where(c => c.ContractNo.StartsWith(viewModel.ContractNo));
            }

            bool queryByDocument = false;
            IQueryable<CDS_Document> documents = models.GetTable<CDS_Document>();
            //wait to fix...查詢合約時, 非聯合承攬狀況, 在部份完成簽署狀態, 會在合約查詢中, 顯示所有合約, 包括未完成簽署的合約
            if (viewModel.QueryStep?.Length > 0)
            {
                documents = documents
                    .Where(d => d.CurrentStep.HasValue)
                    .Where(d => viewModel.QueryStep.Contains((CDS_Document.StepEnum)d.CurrentStep!));
                queryByDocument = true;
            }

            if (!string.IsNullOrEmpty(viewModel.ContractDateFrom))
            {
                DateTime _date;
                DateTime.TryParseExact(viewModel.ContractDateFrom, "yyyy/MM/dd", null,
                    System.Globalization.DateTimeStyles.None, out _date);
                if (_date != null)
                {
                    documents = documents.Where(d => d.DocDate >= _date);
                    queryByDocument = true;
                }
            }
            if (!string.IsNullOrEmpty(viewModel.ContractDateTo))
            {
                DateTime _date;
                DateTime.TryParseExact(viewModel.ContractDateTo, "yyyy/MM/dd", null,
                    System.Globalization.DateTimeStyles.None, out _date);
                if (_date != null)
                {
                    documents = documents.Where(d => d.DocDate < _date.AddDays(1));
                    queryByDocument = true;
                }
            }
            if (queryByDocument)
            {
                items = items.Where(c => documents.Any(d => d.DocID == c.ContractID));
            }

            if (viewModel.Contractor != null)
            {
                var parties = models.GetTable<ContractingUser>()
                                .Where(p => p.UserID == viewModel.Contractor.DecryptKeyValue());
                items = items.Where(c => parties.Any(p => p.ContractID == c.ContractID));
            }

            if (viewModel.PageIndex.HasValue)
            {
                viewModel.PageIndex--;
            }
            else
            {
                viewModel.PageIndex = 0;
            }

            return View("~/Views/Task/VueModule/TaskRequestList.cshtml", items);
        }
        public async Task<ActionResult> AcceptContractAsync([FromBody] SignContractViewModel viewModel)
        {

            //viewModel.KeyID = viewModel.KeyID.GetEfficientString();
            if (viewModel.KeyID != null)
            {
                viewModel.ContractID = viewModel.DecryptKeyValue();
            }

            var contract = models.GetTable<Contract>()
                            .Where(c => c.ContractID == viewModel.ContractID)
                            .FirstOrDefault();


            if (contract == null)
            {
                return Json(new { result = false, message = "合約資料錯誤!!" });
            }

            var profile = await HttpContext.GetUserAsync();
            //var profile = await HttpContext.GetUserProfileUserForTestAsync(4);
            //#region add for postman test
            //if (profile == null)
            //{
            //    profile = models.GetTable<UserProfile>().Where(x => x.UID == viewModel.EncUID.DecryptKeyValue()).FirstOrDefault();
            //}
            //#endregion

            profile = profile.LoadInstance(models);
            if (profile == null || profile.ContractingUser == null)
            {
                return Json(new { result = false, message = "簽約人資料錯誤!!" });
            }

            var requestItem =
                models.GetTable<ContractUserSignatureRequest>()
                    .Where(p => p.ContractID == contract.ContractID)
                    .Where(o => o.UserID == profile.UID)
                    .FirstOrDefault();

            if (requestItem == null)
            {
                return Json(new { result = false, message = "合約資料錯誤!!" });
            }


            //wait to do:用印定義? 挖框決定? 不一定是[文字],[圖章],[日期], 只要符合起約人預期就要可以過, 待補
            //var sealItems = models.GetTable<SealTemplate>().Where(s => s.UID == profile.UID);
            //if (!models.GetTable<ContractSealRequest>()
            //    .Where(s => s.ContractID == contract.ContractID)
            //    .Where(s => sealItems.Any(t => t.SealID == s.SealID))
            //    .Any())
            //{
            //    return Json(new { result = false, message = "簽約人尚未用印!!" });
            //}

            requestItem.StampDate = DateTime.Now;
            models.SubmitChanges();

            if ((UserSession.Get(_httpContextAccessor) != null) && (UserSession.Get(_httpContextAccessor).IsTrust))
            {
                UserSession.Remove(_httpContextAccessor);
                HttpContext.Logout();
            }

            _contractServices.SetModels(models);
            _contractServices.CDS_DocumentTransitStep(contract, profile!.UID, CDS_Document.StepEnum.Sealed, true);
            if (contract.isAllStamped(isTask: true))
            {
                var targetUsers = _contractServices.GetUsersbyContract(contract, true);
                if (ContractServices.IsNotNull(targetUsers) && targetUsers.Count() > 0)
                {
                    _contractServices.SendUsersNotifyEmailAboutContractAsync(
                        contract,
                        _emailContentFactories.GetTaskNotifySign(),
                        targetUsers);
                }
            }

            return Json(new { result = true, dataItem = new { contract.ContractNo, contract.Title } });
        }

        private IQueryable<Contract> ContractSysQueryFilter(UserProfile profile)
        {

            IQueryable<Contract> items =
                models.GetTable<Contract>();

            if (profile.IsSysAdmin())
            {

            }
            else
            {
                items = items.Where(o => models.GetTable<ContractingUser>()
                                                .Where(u => u.UserID == profile.UID)
                                            .Any(u => u.ContractID == o.ContractID) //[用印簽約]可查看
                                    || o.UserProfile.UID == profile.UID//[起約人]可查看(用印簽約者可能不是起約人)
                                    || o.FieldSetUID == profile.UID//[挖框人]可查看(挖框人可能不是起約人)
                                   ) 
                    .Distinct(); 
            }

            return items;
        }


        public async Task<ActionResult> VueListToStampAsync([FromBody] SignContractViewModel viewModel)
        {
            var profile = await HttpContext.GetUserAsync();
            if (profile == null)
            {
                return View("~/Views/Account/Login.cshtml");
            }
            ViewResult result = (ViewResult)(await ListToStampAsync(viewModel));
            result.ViewName = "~/Views/Task/VueModule/TaskRequestList.cshtml";
            return result;
        }

        public async Task<ActionResult> ListToStampIndexAsync(SignContractViewModel viewModel)
        {
            var profile = await HttpContext.GetUserAsync();
            if (profile == null)
            {
                return View("~/Views/Account/Login.cshtml");
            }
            ViewResult result = (ViewResult)(await ListToStampAsync(viewModel));
            result.ViewName = "~/Views/Task/TaskListIndex.cshtml";
            return result;
        }

        [HttpPost]
        public async Task<ActionResult> ListToStampAsync([FromBody] SignContractViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;
            if (viewModel.ContractQueryStep == null) { viewModel.ContractQueryStep = 0; }
            var profile = await HttpContext.GetUserAsync();
            //var profile = await HttpContext.GetUserProfileUserForTestAsync(4);
            profile = profile.LoadInstance(models);
            //#region add for postman test
            //if (profile == null)
            //{
            //    profile = models.GetTable<UserProfile>().Where(x => x.UID == viewModel.EncUID.DecryptKeyValue()).FirstOrDefault();
            //}
            //#endregion
            //var profileCompanyID = 0;
            //var organizationUser = models
            //    .GetTable<OrganizationUser>()
            //    .Where(x => x.UID == profile.UID);

            //if (organizationUser != null && organizationUser.FirstOrDefault() != null)
            //{
            //    profileCompanyID = organizationUser.FirstOrDefault().CompanyID;
            //}

            IQueryable<Contract> items = ContractSysQueryFilter(profile);

            //    //沒有查詢條件預設:0000=0
            //    //是登入者未印:0011=3
            //    //是登入者未簽:0101=5   
            //    //非登入者未印:0010=2
            //    //非登入者未簽:0100=4    

            //public enum QueryStepEnum
            //CurrentUser = 1,  // 0001
            //UnStamped = 2,   // 0010
            //UnSigned = 4,   // 0100
            //UnCommited = 8  // 1000

            items = items.Where(d => !d.CDS_Document.CurrentStep.HasValue
                 || CDS_Document.DocumentEditable.Contains((CDS_Document.StepEnum)d.CDS_Document.CurrentStep!));

            var zzz = viewModel.ContractQueryStep & (int)QueryStepEnum.CurrentUser;
            var yyy = Convert.ToBoolean(viewModel.ContractQueryStep & (int)QueryStepEnum.CurrentUser);

            bool isUserSysAdmin = profile.IsSysAdmin();

            var contractSignatureRequestItems = items
                .SelectMany(x => x.ContractUserSignatureRequest)
                //判斷是查詢登入者的文件, 或是其他人的文件
                .Where(y => (Convert.ToBoolean(viewModel.ContractQueryStep & (int)QueryStepEnum.CurrentUser)) ?
                    (y.UserID == profile.UID || y.Contract.FieldSetUID == profile.UID) : //登入者文件:包括未挖框文件也列示
                    (y.UserID != profile.UID && (y.Contract.FieldSetUID != profile.UID|| y.Contract.FieldSetUID==null))  //其他人文件
                    && !isUserSysAdmin)  //其他人文件
                ;

            //待簽
            //待自己簽:若本人用印, 對方未印, 現行會顯示此筆, 但無法簽署
            //待他人簽:若本人未印, 對方已印, 現行會顯示此筆, 可用印
            if ((Convert.ToBoolean(viewModel.ContractQueryStep & (int)QueryStepEnum.UnSigned)))
            {
                contractSignatureRequestItems = contractSignatureRequestItems
                        .Where(x => (x.SignatureDate == null) && (x.StampDate != null));
            }

            //待用印
            if ((Convert.ToBoolean(viewModel.ContractQueryStep & (int)QueryStepEnum.UnStamped)))
            {
                contractSignatureRequestItems = contractSignatureRequestItems
                    .Where(x=>x.StampDate == null);
            }

            var contractIDs = contractSignatureRequestItems.Select(x => x.ContractID).ToList();
            //符合條件的Contracts
            items = items.Where(y => contractIDs.Contains(y.ContractID));

            viewModel.RecordCount = items?.Count();

            var userprofile = models.GetTable<UserProfile>().Where(x => x.PID == profile.PID).FirstOrDefault();

            ViewBag.CanCreateContract = userprofile.CanCreateContract();

            if (viewModel.PageIndex.HasValue)
            {
                viewModel.PageIndex--;
                return View("~/Views/Task/Module/TaskRequestList.cshtml", items);
            }
            else
            {
                viewModel.PageIndex = 0;
                return View("~/Views/Task/Module/TaskRequestQueryResult.cshtml", items);
            }

            //    //StepEnum.Sealing,2
            //    //StepEnum.Sealed,3
            //    //StepEnum.DigitalSigning,4
            //    //StepEnum.DigitalSigned 5
        }

        public async Task<ActionResult> CommitPdfSignatureAsync(SignatureRequestViewModel viewModel)
        {
            var profile = await HttpContext.GetUserAsync();
            //var profile = await HttpContext.GetUserProfileUserForTestAsync(4);
            var seal = models.GetTable<SealTemplate>().Where(s => s.SealID == viewModel.SealID).FirstOrDefault();
            if (seal == null)
            {
                return Json(new { result = false, message = "請選擇印鑑章!!" });
            }

            if (!(viewModel.SealScale > 0))
            {
                return Json(new { result = false, message = "請輸入正確縮放百分比!!" });
            }

            if (!(viewModel.PageIndex >= 0))
            {
                return Json(new { result = false, message = "請選擇用印頁碼!!" });
            }

            if (!(viewModel.MarginLeft >= 0))
            {
                return Json(new { result = false, message = "請設定左邊界位置!!" });
            }

            if (!(viewModel.MarginTop >= 0))
            {
                return Json(new { result = false, message = "請設定上邊界位置!!" });
            }

            ViewResult? result = await AffixSeal(viewModel) as ViewResult;
            if (result == null)
            {
                return Json(new { result = false, message = "資料錯誤!!" });
            }


            //#region add for postman test
            //if (profile == null)
            //{
            //    profile = models.GetTable<UserProfile>().Where(x => x.UID == viewModel.EncUID.DecryptKeyValue()).FirstOrDefault();
            //}
            //#endregion
            void ApplySeal(Contract contract, SealTemplate seal, int? uid, int? pageIndex)
            {
                ContractSealRequest item = new ContractSealRequest
                {
                    ContractID = contract.ContractID,
                    SealID = seal.SealID,
                    StampDate = DateTime.Now,
                    StampUID = uid,
                    SealScale = viewModel.SealScale,
                    MarginLeft = viewModel.MarginLeft,
                    MarginTop = viewModel.MarginTop,
                    PageIndex = pageIndex,
                };

                models.GetTable<ContractSealRequest>().InsertOnSubmit(item);
                models.SubmitChanges();
            }

            if (profile != null)
            {
                Contract contract = (Contract)result.Model!;
                if (viewModel.DoAllPages == true)
                {
                    for (int pageIdx = 0; pageIdx < contract.GetPdfPageCount(); pageIdx++)
                    {
                        ApplySeal(contract, seal, profile.UID, pageIdx);
                    }
                }
                else
                {
                    ApplySeal(contract, seal, profile.UID, viewModel.PageIndex);
                }

                return Ok(_baseResponse);
            }

            return Ok(_baseResponse.ErrorMessage("請重新登入"));

        }

        [HttpPost]
        public async Task<ActionResult> ResetPdfSignatureAsync(SignatureRequestViewModel viewModel)
        {
            ViewResult? result = await AffixSeal(viewModel) as ViewResult;
            if (result == null)
            {
                return Json(new { result = false, message = "資料錯誤!!" });
            }

            var profile = await HttpContext.GetUserAsync();

            if (profile != null)
            {
                Contract contract = (Contract)result.Model!;

                var table = models.GetTable<ContractSealRequest>();
                var items = table.Where(s => s.StampUID == profile.UID)
                                //iris:因挖框和圖章現行沒有做關連, 有圖章時挖框無法得知, 只能圖章和挖框都顯示, 設定User下的Seal及Note全清除
                                //.Where(s => s.PageIndex == viewModel.PageIndex)
                                .Where(s => s.ContractID == contract.ContractID);
                table.DeleteAllOnSubmit(items);
                models.SubmitChanges();

                var noteTable = models.GetTable<ContractNoteRequest>();
                var notes = noteTable.Where(s => s.StampUID == profile.UID)
                                //iris:因挖框和圖章現行沒有做關連, 有圖章時挖框無法得知, 只能圖章和挖框都顯示, 設定User下的Seal及Note全清除
                                //.Where(s => s.PageIndex == viewModel.PageIndex)
                                .Where(s => s.ContractID == contract.ContractID);
                noteTable.DeleteAllOnSubmit(notes);
                models.SubmitChanges();

                return Json(new { result = true });
            }

            return Json(new { result = false });

        }

        [HttpPost]
        public async Task<IActionResult> FeildSettingsAsync([FromBody] GetFieldSettingRequest req)
        {
            _contractServices.SetModels(models);
            Contract contract = _contractServices.GetContractByID(contractID: req.ContractID.DecryptKeyValue());

            _baseResponse.Data =
                contract
                .ContractSignaturePositionRequest
                .Where(y => y.OperatorID == req.OperatorID.DecryptKeyValue())
                                .Select(x => new ContractConsoleController.ContractSignaturePositionRequest()
                                {
                                    RequestID = x.RequestID,
                                    CompanyID = x.ContractorID.EncryptKey(),
                                    PositionID = x.PositionID,
                                    ScaleWidth = x.ScaleWidth,
                                    ScaleHeight = x.ScaleHeight,
                                    MarginTop = x.MarginTop,
                                    MarginLeft = x.MarginLeft,
                                    Type = x.Type,
                                    PageIndex = x.PageIndex,
                                    OperatorID = x.OperatorID
                                });

            return Ok(_baseResponse);
        }

        [HttpPost]
        public async Task<IActionResult> FeildSettingsUpdateAsync([FromBody] PostFieldSettingRequest req)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            var profile = await HttpContext.GetUserAsync();
            //var profile = await HttpContext.GetUserProfileUserForTestAsync(4);

            //#region add for postman test
            //if (profile == null)
            //{
            //    profile = models.GetTable<UserProfile>().Where(x => x.UID == req.EncUID.DecryptKeyValue()).FirstOrDefault();
            //}
            //#endregion
            _contractServices.SetModels(models);
            var contractID = req.ContractID.DecryptKeyValue();
            Contract contract = _contractServices.GetContractByID(contractID: contractID);
            //wait to do:如果是流覽step16, 會判斷有誤
            //if (contract.CDS_Document.CurrentStep >= 5)
            if (!CDS_Document.DocumentCanFeildSet.Contains((CDS_Document.StepEnum)contract.CDS_Document.CurrentStep!))
            {
                return Ok(_baseResponse.ErrorMessage("文件已進行中,無法修改資料"));
            }

            if (contract.IsPassStamp??false)
            {
                return Ok(_baseResponse.ErrorMessage("文件設定為直接簽署"));
            }

            _contractServices.DeleteAndCreateFieldPostion(contract, req.FieldSettings);
            models.SubmitChanges();
            _contractServices.CDS_DocumentTransitStep(contract, profile!.UID, CDS_Document.StepEnum.FieldSet, true);
            return Json(_baseResponse);
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult> Trust(string token)
        {
            _contractServices.SetModels(models);
            (BaseResponse resp, JwtToken jwtTokenObj, UserProfile userProfile)
                    = _contractServices.TokenValidate(JwtTokenValidator.Base64UrlDecodeToString(token).DecryptData());

            if (resp.HasError)
            {
                resp.Url = $"{Properties.Settings.Default.WebAppDomain}";
                return View("~/Views/Shared/CustomMessage.cshtml", resp);
            }

            if (string.IsNullOrEmpty(jwtTokenObj.ContractID))
            {
                return View("~/Views/Shared/CustomMessage.cshtml", "contractID is null.");
            }

            //wait to do:Trust進來可能沒有正常user權限,
            //但因為controller都有用var profile = await HttpContext.GetUserAsync();, 暫時先用
            await HttpContext.SignOnAsync(userProfile);
            var userSession = UserSession.Create(_httpContextAccessor);

            if (jwtTokenObj.IsFieldSet)
            {
                return await FieldSettingView(new SignatureRequestViewModel() { IsTrust = true, KeyID = jwtTokenObj.ContractID });
            }

            if (jwtTokenObj.IsSeal)
            {
                return await AffixSeal(new SignatureRequestViewModel() { IsTrust = true, KeyID = jwtTokenObj.ContractID });
            }

            if (jwtTokenObj.IsSign)
            {
                _contractServices.SetModels(models);
                (resp, Contract contract) =
                    _contractServices.CanTaskDigitalSign(contractID: jwtTokenObj.ContractID.DecryptKeyValue(), profile:userProfile);

                if (resp.HasError)
                {
                    resp.Url = $"{Properties.Settings.Default.WebAppDomain}";
                    return View("~/Views/Shared/CustomMessage.cshtml", resp);
                }

                DigitalSignModal digitalSignModal = new DigitalSignModal()
                {

                    ContractNo = contract.ContractNo,
                    ContractTitle = contract.Title,
                    //CompanyName = string.Empty,
                    ContractID = jwtTokenObj.ContractID
                };


                return View("~/Views/Task/DigitalSignModal.cshtml", digitalSignModal);

            }

            this.HttpContext.Logout();
            return RedirectToAction("Login", "Account");
        }

        public async Task<ActionResult> FieldSettingView(SignatureRequestViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;

            int contractID = viewModel.ContractID ?? 0;
            if (viewModel.KeyID != null)
            {
                contractID = viewModel.DecryptKeyValue();
            }
            if (contractID == 0)
            {
                return View("~/Views/Shared/CustomMessage.cshtml", _baseResponse.ErrorMessage("contractID is null."));
            }

            var profile = await HttpContext.GetUserAsync();
            //#region add for postman test
            //if (profile == null)
            //{
            //    //profile = models.GetTable<UserProfile>().Where(x => x.UID == 4).FirstOrDefault();
            //    profile = models.GetTable<UserProfile>().Where(x => x.UID == 41).FirstOrDefault();
            //}
            //#endregion
            _contractServices.SetModels(models);
            (BaseResponse resp, Contract contract) =
                 _contractServices.CanFieldSet(contractID: contractID, profile: profile);

            if (resp.HasError)
            {
                resp.Url = $"{Properties.Settings.Default.ContractListUrl}";
                if (viewModel.IsTrust != null && viewModel.IsTrust == true)
                {
                    resp.Url = $"{Properties.Settings.Default.WebAppDomain}";
                }
                return View("~/Views/Shared/CustomMessage.cshtml", resp);
            }

            return View("~/Views/Task/Stamper.cshtml", contract);

        }

        public async Task<ActionResult> AffixSeal(SignatureRequestViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;

            int contractID = viewModel.ContractID ?? 0;
            if (viewModel.KeyID != null)
            {
                contractID = viewModel.DecryptKeyValue();
            }
            if (contractID == 0)
            {
                return View("~/Views/Shared/CustomMessage.cshtml", _baseResponse.ErrorMessage("contractID is null."));
            }

            var profile = await HttpContext.GetUserAsync();
            //#region add for postman test
            //if (profile == null)
            //{
            //    //profile = models.GetTable<UserProfile>().Where(x => x.UID == 4).FirstOrDefault();
            //    profile = models.GetTable<UserProfile>().Where(x => x.UID == 41).FirstOrDefault();
            //}
            //#endregion
            _contractServices.SetModels(models);
            (BaseResponse resp, Contract contract) =
                 _contractServices.CanTaskSeal(contractID: contractID, userProfile: profile);

            if (resp.HasError)
            {
                resp.Url = $"{ContractHome.Properties.Settings.Default.ContractListUrl}";
                if (viewModel.IsTrust != null && viewModel.IsTrust == true)
                {
                    resp.Url = $"{ContractHome.Properties.Settings.Default.WebAppDomain}";
                }
                return View("~/Views/Shared/CustomMessage.cshtml", resp);
            }

            return View("~/Views/Task/Stamper.cshtml", contract);

        }

        [HttpPost]
        public async Task<IActionResult> GetOperatorsAsync()
        {
            //var profile = await HttpContext.GetUserAsync();
            var profile = await HttpContext.GetUserProfileUserForTestAsync(4);
            if (profile != null) 
            {
                profile = profile.LoadInstance(models);
            }
            //#region add for postman test
            //if (profile == null)
            //{
            //    profile = models.GetTable<UserProfile>().Where(x => x.UID == 4).FirstOrDefault();
            //}
            //#endregion
            if (profile == null)
            {
                return Json(new BaseResponse(true, "請重新登入"));
            }

            _contractServices.SetModels(models);

            IEnumerable<UserProfile>? operators
                = _contractServices.GetOperatorsByUID(profile.UID);

            IEnumerable<UserProfile>? users
                = _contractServices.GetUsersbyCompanyID(profile.CompanyID);

            IEnumerable<Models.Operator> nonOperators = 
                users.Select(x => new Models.Operator(pID: x.UID.EncryptKey(), email: ContractServices.EmailMasking(x.EMail), title: x.PID, region: x.Region, isOperator: false));

            _baseResponse.Data = operators.Select(x =>
                new Models.Operator(pID: x.UID.EncryptKey(), email: ContractServices.EmailMasking(x.EMail), title: x.OperatorNote, region: x.Region, isOperator: true))
                .Concat(nonOperators).Distinct();

            return Json(_baseResponse);
        }

        [HttpPost]
        public async Task<ActionResult> EstablishAsync([FromBody] GetSignatoriesRequest req)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            Contract contract;
            UserProfile profile = await HttpContext.GetUserAsync();
            //UserProfile profile = await HttpContext.GetUserProfileUserForTestAsync(4);

            _contractServices.SetModels(models: models);
            contract = _contractServices.GetContractByID(req.ContractID.DecryptKeyValue());

            if (ContractServices.IsNotNull(profile))
            {
                _contractServices.CDS_DocumentTransitStep(contract, profile.UID, CDS_Document.StepEnum.Establish,true);

                if (contract.IsPassStamp == true)
                {
                    _contractServices.CDS_DocumentTransitStep(contract, profile.UID, CDS_Document.StepEnum.Sealed, true);
                }

                //3.發送通知(one by one)
                var targetUsers = _contractServices.GetUsersbyContract(contract, true);
                if (ContractServices.IsNotNull(targetUsers))
                {
                    _contractServices.SendUsersNotifyEmailAboutContractAsync(
                        contract,
                        (contract.IsPassStamp == true) ? _emailContentFactories.GetTaskNotifySign() : _emailContentFactories.GetTaskNotifySeal(),
                        targetUsers);
                }

                return Json(_baseResponse);
            }

            return Json(_baseResponse.ErrorMessage());
        }

        [HttpPost]
        //是否有Contract產製修改權限ContractHome.Security.Authorization
        public async Task<ActionResult> ConfigAsync([FromBody] PostConfigRequest req)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            //UserProfile profile = await HttpContext.GetUserAsync();
            UserProfile profile = await HttpContext.GetUserProfileUserForTestAsync(4);
            
            _contractServices.SetModels(models);
            var contractID = req.ContractID.DecryptKeyValue();
            Contract? contract = _contractServices.GetContractByID(contractID: contractID);
            bool isFieldSetUserSameWithInitiator = true;
            if (ContractServices.IsNull(contract))
            {
                ModelState.AddModelError("", "合約不存在");
                return BadRequest();
            }
            //wait to do:如果是流覽step16, 會判斷有誤
            //if (contract.CDS_Document.CurrentStep >= 1)
            if (!CDS_Document.DocumentCanConfig.Contains((CDS_Document.StepEnum)contract.CDS_Document.CurrentStep!))
            {
                ModelState.AddModelError("", "合約已進行中,無法修改資料");
                return BadRequest();
            }

            if (ContractServices.IsNotNull(profile))
            {
                _contractServices.SetConfigAndSave(contract, req, profile.UID, true);
                //新增IsPassStamp判斷
                if (!contract.CreateUID.Equals(contract.FieldSetUID))
                {
                    isFieldSetUserSameWithInitiator = false;
                    IQueryable<UserProfile> targetUsers = _contractServices.GetUserByUID(contract.FieldSetUID)!;
                    if (ContractServices.IsNotNull(targetUsers))
                    {
                        _contractServices.SendUsersNotifyEmailAboutContractAsync(
                            contract,
                            _emailContentFactories.GetTaskNotifyFieldSet(),
                            targetUsers);
                    }
                }

                _baseResponse.Data = new { ContractID = contract.ContractID.EncryptKey(), IsGoToFieldSet = isFieldSetUserSameWithInitiator };
                return Json(_baseResponse);
            }

            return Json(_baseResponse.ErrorMessage());
        }

        public async Task<ActionResult> CommitPdfNoteAsync(SignatureRequestViewModel viewModel)
        {
            //viewModel.Note = viewModel.Note?.GetEfficientString();
            if (viewModel.Note == null)
            {
                return Json(new { result = false, message = "請輸入文字內容!!" });
            }

            if (!(viewModel.PageIndex >= 0))
            {
                return Json(new { result = false, message = "請選擇用印頁碼!!" });
            }

            if (!(viewModel.MarginLeft >= 0))
            {
                return Json(new { result = false, message = "請設定左邊界位置!!" });
            }

            if (!(viewModel.MarginTop >= 0))
            {
                return Json(new { result = false, message = "請設定上邊界位置!!" });
            }

            ViewResult? result = await AffixSeal(viewModel) as ViewResult;
            if (result == null)
            {
                return Json(new { result = false, message = "資料錯誤!!" });
            }

            var profile = await HttpContext.GetUserAsync();

            void ApplyNote(Contract contract, int? uid, int? pageIndex)
            {
                ContractNoteRequest item = new ContractNoteRequest
                {
                    ContractID = contract.ContractID,
                    StampDate = DateTime.Now,
                    StampUID = profile.UID,
                    SealScale = viewModel.SealScale,
                    MarginLeft = viewModel.MarginLeft,
                    MarginTop = viewModel.MarginTop,
                    PageIndex = pageIndex,
                    Note = viewModel.Note,
                };

                models.GetTable<ContractNoteRequest>().InsertOnSubmit(item);
                models.SubmitChanges();
            }

            if (profile != null)
            {
                Contract contract = (Contract)result.Model!;
                if (viewModel.DoAllPages == true)
                {
                    for (int pageIdx = 0; pageIdx < contract.GetPdfPageCount(); pageIdx++)
                    {
                        ApplyNote(contract, profile.UID, pageIdx);
                    }
                }
                else
                {
                    ApplyNote(contract, profile.UID, viewModel.PageIndex);
                }

                return Json(new { result = true });
            }

            return Json(new { result = false });

        }

        [RequestSizeLimit(200 * 1024 * 1024)]
        public async Task<IActionResult> Create(IFormFile file)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            // var user = await HttpContext.GetUserAsync();
            // //var profile = user.LoadInstance(models);
            // if (!ContractServices.IsNotNull(user) || !ContractServices.IsNotNull(user.OrganizationUser))
            // {
            //   return Ok(_baseResponse.ErrorMessage("請重新登入"));
            // }
            // if (!ContractServices.IsNotNull(file))
            // {
            //   return Ok(_baseResponse.ErrorMessage("請選擇檔案!!"));
            // }

            // String extName = Path.GetExtension(file.FileName).ToLower();
            // if (extName != ".pdf")
            // {
            //   return Ok(_baseResponse.ErrorMessage("合約檔案類型只能是pdf!!"));
            // }

            Contract contract = new Contract
            {
                FilePath = file.StoreContractDocument(),
                CompanyID = 1,
                ContractNo = String.Empty,
                CDS_Document = new CDS_Document
                {
                    DocDate = DateTime.Now,
                    CurrentStep = (int)CDS_Document.StepEnum.Initial,
                    ProcessType = (int)CDS_Document.ProcessTypeEnum.PDF,
                },
            };

            models.GetTable<Contract>().InsertOnSubmit(contract);
            models.SubmitChanges();
            _baseResponse.Data = new { ContractID = contract.ContractID.EncryptKey() };
            return Ok(_baseResponse);

        }
    }
}
