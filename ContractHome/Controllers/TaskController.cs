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
using static ContractHome.Controllers.ContractConsoleController;
using static ContractHome.Helper.JwtTokenGenerator;

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

            var profile = (await HttpContext.GetUserAsync()).LoadInstance(models);
            if (profile?.ContractingUser == null)
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
            if (contract.isAllStamped())
            {
                var targetUsers = _contractServices.GetUsersbyContract(contract);
                if (ContractServices.IsNotNull(targetUsers))
                {
                    _contractServices.CDS_DocumentTransitStep(contract, profile!.UID, CDS_Document.StepEnum.Sealed);
                    _contractServices.SendUsersNotifyEmailAboutContractAsync(
                        contract,
                        _emailContentFactories.GetNotifySign(),
                        targetUsers);
                }
            }
            else
            {
                _contractServices.CDS_DocumentTransitStep(contract, profile!.UID, CDS_Document.StepEnum.Sealing);
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
                items = items.Where(c => models.GetTable<ContractingUser>()
                                    .Where(o=> o.UserID == profile.UID)
                                    .Any(p => p.ContractID == c.ContractID));
            }

            return items;
        }

        public async Task<ActionResult> ListToStampAsync(SignContractViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;
            if (viewModel.ContractQueryStep == null) { viewModel.ContractQueryStep = 0; }

            var profile = await HttpContext.GetUserAsync();
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

            //    //StepEnum.Sealing,2
            //    //StepEnum.Sealed,3
            //    //StepEnum.DigitalSigning,4
            //    //StepEnum.DigitalSigned 5

            items = items.Where(d => !d.CDS_Document.CurrentStep.HasValue
                 || CDS_Document.DocumentEditable.Contains((CDS_Document.StepEnum)d.CDS_Document.CurrentStep!));

            var contractSignatureRequestItems = items
                .SelectMany(x => x.ContractUserSignatureRequest)
                //判斷是查詢登入者的合約, 或是其他人的合約
                .Where(y => (Convert.ToBoolean(viewModel.ContractQueryStep & (int)QueryStepEnum.CurrentUser)) ?
                    (y.UserID == profile.UID) : //登入者的合約
                    (y.UserID != profile.UID))  //其他人的合約
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
                    .Where(x => x.StampDate == null);
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
                return View("~/Views/ContractConsole/Module/ContractRequestList.cshtml", items);
            }
            else
            {
                viewModel.PageIndex = 0;
                return View("~/Views/ContractConsole/Module/ContractRequestQueryResult.cshtml", items);
            }
        }

        public async Task<ActionResult> CommitPdfSignatureAsync(SignatureRequestViewModel viewModel)
        {
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

            var profile = await HttpContext.GetUserAsync();

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

                return Json(new { result = true });
            }

            return Json(new { result = false });

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
            var profile = await HttpContext.GetUserAsync();
            #region add for postman test
            if (profile == null)
            {
                profile = models.GetTable<UserProfile>().Where(x => x.UID == req.EncUID.DecryptKeyValue()).FirstOrDefault();
            }
            #endregion
            _contractServices.SetModels(models);
            var contractID = req.ContractID.DecryptKeyValue();
            Contract contract = _contractServices.GetContractByID(contractID: contractID);
            //wait to do...獨立控管每項作業可執行step
            if (contract.CDS_Document.CurrentStep >= 5)
            {
                return Json(new BaseResponse(true, "合約已進行中,無法修改資料"));
            }
            //wait to do..檢查currentStep是否可進行UpdateFieldSetting
            //contract.CDS_Document.CheckIfCanGo
            _contractServices.DeleteAndCreateFieldPostion(contract, req.FieldSettings);
            models.SubmitChanges();
            _contractServices.CDS_DocumentTransitStep(contract, profile!.UID, CDS_Document.StepEnum.FieldSet);
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
            HttpContext.SignOnAsync(userProfile);
            var userSession = UserSession.Create(_httpContextAccessor);

            if (jwtTokenObj.IsSeal)
            {
                return await AffixSeal(new SignatureRequestViewModel() { IsTrust = true, KeyID = jwtTokenObj.ContractID });
            }

            if (jwtTokenObj.IsSign)
            {

                _contractServices.SetModels(models);
                (resp, Contract contract, userProfile) =
                     _contractServices.CanPdfDigitalSign(contractID: jwtTokenObj.ContractID.DecryptKeyValue());

                if (resp.HasError)
                {
                    resp.Url = $"{Properties.Settings.Default.WebAppDomain}";
                    return View("~/Views/Shared/CustomMessage.cshtml", resp);
                }

                DigitalSignModal digitalSignModal = new DigitalSignModal()
                {

                    ContractNo = contract.ContractNo,
                    ContractTitle = contract.Title,
                    CompanyName = userProfile.Organization.CompanyName,
                    ContractID = jwtTokenObj.ContractID
                };


                return View("~/Views/Shared/DigitalSignModal.cshtml", digitalSignModal);

            }

            this.HttpContext.Logout();
            return RedirectToAction("Login", "Account");
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

            var profile = HttpContext.GetUserAsync().Result;
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
        public async Task<IActionResult> GetOperatorsAsync([FromBody] GetSignatoriesRequest req)
        {
            var profile = await HttpContext.GetUserAsync();
            #region add for postman test
            if (profile == null && req.EncUID != null)
            {
                profile = models.GetTable<UserProfile>().Where(x => x.UID == req.EncUID.DecryptKeyValue()).FirstOrDefault();
            }
            #endregion
            if (profile == null)
            {
                return Json(new BaseResponse(true, "請重新登入"));
            }

            _contractServices.SetModels(models);
            Contract contract = _contractServices.GetContractByID(contractID: req.ContractID.DecryptKeyValue());
            if (contract == null)
            {
                return Json(new BaseResponse(true, "無此權限"));
            }

            IEnumerable<UserProfile>? operators
                = _contractServices.GetOperatorByCompanyID(contract.CompanyID);

            _baseResponse.Data = operators.Select(x =>
                new
                {
                    id = x.UID.EncryptKey(),
                    title = x.OperatorNote??string.Empty,
                    email = x.EMail
                });

            return Json(_baseResponse);
        }

        [HttpPost]
        public async Task<ActionResult> EstablishAsync([FromBody] GetSignatoriesRequest req)
        {
            Contract contract;
            UserProfile profile = await HttpContext.GetUserAsync();
            #region add for postman test
            if (profile == null && req.EncUID.Length > 0)
            {
                profile = models.GetTable<UserProfile>().Where(x => x.UID == 4).FirstOrDefault();
            }
            #endregion

            _contractServices.SetModels(models: models);
            contract = _contractServices.GetContractByID(req.ContractID.DecryptKeyValue());

            if (ContractServices.IsNotNull(profile))
            {
                _contractServices.CDS_DocumentTransitStep(contract, profile.UID, CDS_Document.StepEnum.Establish);

                if (contract.IsPassStamp == true)
                {
                    _contractServices.CDS_DocumentTransitStep(contract, profile.UID, CDS_Document.StepEnum.Sealed);
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
            UserProfile profile = await HttpContext.GetUserAsync();
#if DEBUG
            if (profile == null && req.EncUID != null)
            {
                profile = models.GetTable<UserProfile>().Where(x => x.UID == req.EncUID.DecryptKeyValue()).FirstOrDefault();
            }
#endif

            _contractServices.SetModels(models);
            var contractID = req.ContractID.ToString().DecryptKeyValue();
            Contract? contract = _contractServices.GetContractByID(contractID: contractID);
            if (!ContractServices.IsNotNull(contract))
            {
                ModelState.AddModelError("", "合約不存在");
                return BadRequest();
            }
            //wait to do...獨立控管每項作業可執行step
            if (contract.CDS_Document.CurrentStep >= 1)
            {
                ModelState.AddModelError("", "合約已進行中,無法修改資料");
                return BadRequest();
            }

            if (ContractServices.IsNotNull(profile))
            {
                _contractServices.SetConfigAndSave(contract, req, profile.UID, true);
                return Json(_baseResponse);
            }

            return Json(_baseResponse.ErrorMessage());
        }

        [RequestSizeLimit(200 * 1024 * 1024)]
        public async Task<IActionResult> Create(IFormFile file)
        {
            var user = await HttpContext.GetUserAsync();
            #region add for postman test
            if (!ContractServices.IsNotNull(user))
            {
                user = models.GetTable<UserProfile>().Where(x => x.UID == 4).FirstOrDefault();
            }
            #endregion
            var profile = user.LoadInstance(models);
            if (!ContractServices.IsNotNull(profile)||!ContractServices.IsNotNull(profile.OrganizationUser))
            {
                return Ok(_baseResponse.ErrorMessage("請重新登入"));
            }
            if (!ContractServices.IsNotNull(file))
            {
                return Ok(_baseResponse.ErrorMessage("請選擇檔案!!"));
            }

            String extName = Path.GetExtension(file.FileName).ToLower();
            if (extName != ".pdf")
            {
                return Ok(_baseResponse.ErrorMessage("合約檔案類型只能是pdf!!"));
            }

            Contract contract = new Contract
            {
                FilePath = file.StoreContractDocument(),
                CompanyID = profile.CompanyID??0,
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
