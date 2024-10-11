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

        [HttpPost]
        public async Task<IActionResult> GetFeildPositionAsync([FromBody] GetFieldSettingRequest req)
        {
            _contractServices.SetModels(models);
            var ttt = req.ContractID.DecryptKeyValue();
            Contract contract = _contractServices.GetContractByID(contractID: req.ContractID.DecryptKeyValue());

            _baseResponse.Data =
                contract
                .ContractSignaturePositionRequest
                .Where(y => y.OperatorID == req.OperatorID.DecryptKeyValue());

            return Json(_baseResponse);
        }

        [HttpPost]
        public async Task<IActionResult> FeildPositionAsync([FromBody] PostFieldSettingRequest req)
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
                //return View("SignatureTrust",resp);
                throw new ArgumentException(resp.Message);
            }

            if (string.IsNullOrEmpty(jwtTokenObj.ContractID))
            {
                throw new ArgumentException("contractID is null.");
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

            return View("~/Views/ContractConsole/AffixPdfSealImage.cshtml", contract);

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
