using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ContractHome.Models.DataEntity;
using ContractHome.Models.ViewModel;
using CommonLib.Utility;
using ContractHome.Helper;
using CommonLib.Core.Utility;
using System.Drawing;
using System.Linq.Dynamic.Core;
using ContractHome.Models.Helper;
using System.Data.Linq;
using System.Data;
using ContractHome.Helper.DataQuery;
using System.Web;
using Newtonsoft.Json;
using ContractHome.Models.Email.Template;
using static ContractHome.Models.DataEntity.CDS_Document;
using ContractHome.Models.Dto;
using FluentValidation;
using static ContractHome.Models.Helper.ContractServices;
using static ContractHome.Helper.JwtTokenGenerator;
using ContractHome.Models.Cache;
using ContractHome.Properties;
using static CommonLib.Utility.PredicateBuilder;
using Newtonsoft.Json.Linq;
using System.Reflection;
using ClosedXML.Excel;

namespace ContractHome.Controllers
{
    //remark for testing by postman
    //[Authorize]
    public class ContractConsoleController : SampleController
    {
        private readonly ILogger<HomeController> _logger;
        private ContractServices? _contractServices;
        private BaseResponse baseResponse = new BaseResponse();
        private readonly ICacheStore _cacheStore;

        public ContractConsoleController(ILogger<HomeController> logger,
            IServiceProvider serviceProvider,
            ICacheStore cacheStore,
            ContractServices contractServices
          ) : base(serviceProvider)
        {
            _logger = logger;
            _contractServices = contractServices;
            _cacheStore = cacheStore;
        }

        public IActionResult ApplyContract(TemplateResourceViewModel viewModel)
        {
            return View();
        }

        public IActionResult SaveContract(SignContractViewModel viewModel, IFormFile contractDoc)
        {
            if (contractDoc == null)
            {
                return Json(new { result = false, message = "請選擇檔案!!" });
            }

            String extName = Path.GetExtension(contractDoc.FileName).ToLower();
            if (extName != ".docx" && extName != ".pdf")
            {
                return Json(new { result = false, message = "合約檔案類型只能是MS Word或pdf!!" });
            }

            viewModel.ContractNo = viewModel.ContractNo.GetEfficientString();
            if (viewModel.ContractNo == null)
            {
                ModelState.AddModelError("ContractNo", "請輸入合約編號!!");
            }

            if (!viewModel.InitiatorID.HasValue)
            {
                ModelState.AddModelError("Initiator", "請選擇合約發起人!!");
            }

            if (!viewModel.ContractorID.HasValue)
            {
                ModelState.AddModelError("Contractor", "請選擇簽約人!!");
            }

            if (!viewModel.InitiatorIntent.HasValue)
            {
                ModelState.AddModelError("InitiatorIntent", "請選擇合約發起人身份!!");
            }
            else if (!viewModel.ContractorIntent.HasValue)
            {
                ModelState.AddModelError("ContractorIntent", "請選擇簽約人身份!!");
            }
            else if (viewModel.InitiatorIntent == viewModel.ContractorIntent)
            {
                ModelState.AddModelError("InitiatorIntent", "合約發起人與簽約人不能同一身份!!");
                ModelState.AddModelError("ContractorIntent", "合約發起人與簽約人不能同一身份!!");
            }

            if (!ModelState.IsValid)
            {
                return View("~/Views/Shared/ReportInputError.cshtml");
            }

            Contract contract = new Contract
            {
                FilePath = contractDoc.StoreContractDocument(),
                ContractNo = viewModel.ContractNo,
                CDS_Document = new CDS_Document
                {
                    DocDate = DateTime.Now,
                },
            };

            if (extName == ".docx")
            {
                //ComponentInfo.SetLicense(Settings.Default.GemboxKey);

                //// Load Word document from file's path.
                //var document = DocumentModel.Load(contract.FilePath);
                //var elements = document.GetChildElements(true, ElementType.Picture).ToList();
                //if (elements?.Count > 0)
                //{
                //    foreach (var element in elements)
                //    {
                //        var picture = (Picture)element;
                //        var template = picture.TryToMatchTemplate(models);
                //        if (template != null)
                //        {
                //            contract.ContractSealRequest.Add(new ContractSealRequest
                //            {
                //                SealID = template.SealID,
                //            });
                //        }
                //    }
                //}
                //contract.CDS_Document.ProcessType = (int)CDS_Document.ProcessTypeEnum.DOCX;

                contract.FilePath = contract.FilePath.ConvertDocxToPdf();
            }
            //else
            {
                contract.ContractSignatureRequest.Add(new ContractSignatureRequest
                {
                    CompanyID = viewModel.InitiatorID!.Value,
                });

                contract.ContractSignatureRequest.Add(new ContractSignatureRequest
                {
                    CompanyID = viewModel.ContractorID!.Value,
                });

                contract.CDS_Document.ProcessType = (int)CDS_Document.ProcessTypeEnum.PDF;
            }

            contract.ContractingParty.Add(new ContractingParty
            {
                CompanyID = viewModel.InitiatorID.Value,
                IntentID = viewModel.InitiatorIntent!.Value,
                IsInitiator = true,
            });

            contract.ContractingParty.Add(new ContractingParty
            {
                CompanyID = viewModel.ContractorID.Value,
                IntentID = viewModel.ContractorIntent!.Value,
            });

            models.GetTable<Contract>().InsertOnSubmit(contract);
            models.SubmitChanges();

            return Json(new { result = true });
        }


        public async Task<ActionResult> CommitContractAsync([FromBody] SignContractViewModel viewModel)
        {
            var profile = await HttpContext.GetUserAsync();
            int? uid = profile?.UID;

            if (uid == null)
            {
                return Json(new { result = false, message = "身份驗證失敗." });
            }

            if (!viewModel.InitiatorID.HasValue)
            {
                ModelState.AddModelError("Initiator", "請選擇合約發起人!!");
            }

            if (!(viewModel.Contractors?.Length > 0))
            {
                ModelState.AddModelError("Contractor", "請選擇簽約人!!");
            }
            else
            {
                //wait to do:未做TRANSACTION處理, 檢查提前做, 避免只完成部份Contractor
                for (int i = 0; i < viewModel.Contractors!.Length; i++)
                {
                    if (string.IsNullOrEmpty(viewModel.Contractors[i].Contractor)
                        || string.IsNullOrEmpty(viewModel.Initiator))
                    {
                        ModelState.AddModelError("Initiator", "起約人或簽約人ID空白!!");
                        break;
                    }

                    var contractorID = viewModel.Contractors[i].ContractorID;
                    var initiatorID = viewModel.InitiatorID!.Value;

                    if (contractorID == initiatorID)
                    {
                        ModelState.AddModelError("Initiator", $"起約人{initiatorID}不可和簽約對象{contractorID}相同.");
                    }
                }
            }

            viewModel.KeyID = viewModel.KeyID.GetEfficientString();
            if (viewModel.KeyID != null)
            {
                viewModel.ContractID = viewModel.DecryptKeyValue();
            }


            if (!ModelState.IsValid)
            {
                return Json(new { result = false, message = ModelState.ErrorMessage() });
            }

            DataLoadOptions ops = new DataLoadOptions();
            ops.LoadWith<CDS_Document>(c => c.Contract);
            ops.LoadWith<Contract>(c => c.ContractSealRequest);
            models.LoadOptions = ops;

            var contract = models.GetTable<Contract>()
                            .Where(c => c.ContractID == viewModel.ContractID)
                            .FirstOrDefault();


            if (contract?.FilePath == null || !System.IO.File.Exists(contract.FilePath))
            {
                return Json(new { result = false, message = "合約資料錯誤!!" });
            }

            //remark for postman test
            if (!contract.ContractSealRequest.Any() && viewModel.IgnoreSeal != true)
            {
                return Json(new { result = false, message = "合約未用印!!" });
            }

            contract.ContractContent = new Binary(System.IO.File.ReadAllBytes(contract.FilePath));


            models.SubmitChanges();
            try
            {
                _contractServices?.SetModels(models);
                List<Contract> notifyList = new List<Contract>() { contract };
                if (viewModel.Contractors!.Length == 1)
                {
                    var contractorID = viewModel.Contractors[0].ContractorID;
                    var initiatorID = viewModel.InitiatorID!.Value;
                    _contractServices?.CreateAndSaveParty(
                        initiatorID: initiatorID,
                        contractorID: contractorID ?? 0,
                        contract: contract,
                        viewModel.Contractors[0].SignaturePositions,
                        uid ?? 0);

                    //return Json(new { result = true, dataItem = new { contract.ContractNo, contract.Title } });
                }
                else
                {
                    for (int i = 0; i < viewModel.Contractors!.Length; i++)
                    {
                        var contractorID = viewModel.Contractors[i].ContractorID;
                        var initiatorID = viewModel.InitiatorID!.Value;
                        //wait to do..個別簽署暫關閉待調整
                        //if ((contract.IsJointContracting == true) || (i == 0))
                        //{
                        //    _contractServices?.CreateAndSaveParty(
                        //        initiatorID: initiatorID,
                        //        contractorID: contractorID ?? 0,
                        //        contract: contract,
                        //        viewModel.Contractors[i].SignaturePositions,
                        //        uid ?? 0);

                        //}
                        //else
                        //{
                        //    var newContract = _contractServices.CreateAndSaveContractByOld(contract);

                        //    _contractServices.CreateAndSaveParty(
                        //        initiatorID: initiatorID,
                        //        contractorID: contractorID ?? 0,
                        //        contract: newContract,
                        //        viewModel.Contractors[i].SignaturePositions,
                        //        uid ?? 0
                        //    );
                        //    notifyList.Add(newContract);
                        //}

                    }
                }

                _contractServices.SaveContract();

                //wait to do..CommitContractAsync for 個別簽署功能待確認
                //if (notifyList.Count > 0)
                //{
                //    _contractServices.SendNotifyEmailAsync(
                //        notifyList, EmailBody.EmailTemplate.NotifySeal);
                //}
            }
            catch (Exception ex)
            {
                FileLogger.Logger.Error(ex);
                return Json(new { result = false, message = "合約建立失敗." });
            }



            return Json(new { result = true, dataItem = new { contract.ContractNo, contract.Title } });
        }

        public async Task<ActionResult> AcceptContractAsync([FromBody] SignContractViewModel viewModel)
        {

            viewModel.KeyID = viewModel.KeyID.GetEfficientString();
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
            if (profile?.OrganizationUser == null)
            {
                return Json(new { result = false, message = "簽約人資料錯誤!!" });

            }

            var requestItem =
                models.GetTable<ContractSignatureRequest>()
                    .Where(p => p.ContractID == contract.ContractID)
                    .Where(o => o.CompanyID == profile.OrganizationUser.CompanyID)
                    .FirstOrDefault();

            if (requestItem == null)
            {
                return Json(new { result = false, message = "合約未用印!!" });
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

            if (contract.isAllStamped())
            {
                contract.CDS_Document.TransitStep(models, profile!.UID, CDS_Document.StepEnum.Sealed);
                _contractServices?.SetModels(models);
                _contractServices?.SendContractNotifyEmailAsync(contract, EmailBody.EmailTemplate.NotifySign);
            } 
            else
            {
                contract.CDS_Document.AddDocumentProcessLog(models, profile!.UID, CDS_Document.StepEnum.Sealed);
            }

            return Json(new { result = true, dataItem = new { contract.ContractNo, contract.Title } });
        }

        public async Task<ActionResult> LoadSignatureRequestAsync(SignContractViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;

            var result = LoadContract(viewModel);
            Contract? contract = ViewBag.Contract as Contract;

            if (contract == null)
            {
                return result;
            }

            var profile = (await HttpContext.GetUserAsync()).LoadInstance(models);
            if (profile?.OrganizationUser == null)
            {
                return Json(new { result = false, message = "合約簽署人資料錯誤!!" });
            }

            var requestItem =
                models.GetTable<ContractSignatureRequest>()
                    .Where(p => p.ContractID == contract.ContractID)
                    .Where(o => o.CompanyID == profile.OrganizationUser.CompanyID)
                    .FirstOrDefault();

            if (requestItem == null)
            {
                return Json(new { result = false, message = "合約未建立!!" });
            }

            ViewBag.SignatureRequest = requestItem;
            ViewBag.Contract = contract;
            ViewBag.Profile = profile;

            return Json(new { result = true, dataItem = new { contract.ContractNo, contract.Title } });
        }

        public ActionResult LoadContract(SignContractViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;

            viewModel.KeyID = viewModel.KeyID.GetEfficientString();
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

            ViewBag.Contract = contract;

            return Json(new { result = true, dataItem = new { contract.ContractNo, contract.Title } });
        }

        [Flags]
        public enum QueryStepEnum
        {
            CurrentUser = 1,  // 0001
            UnStamped = 2,   // 0010
            UnSigned = 4,   // 0100
            UnCommited = 8  // 1000
        }

        public async Task<ActionResult> ListToStampAsync(SignContractViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;
            if (viewModel.ContractQueryStep == null) { viewModel.ContractQueryStep = 0; }

            var profile = await HttpContext.GetUserAsync();
            var profileCompanyID = 0;
            var organizationUser = models
                .GetTable<OrganizationUser>()
                .Where(x => x.UID == profile.UID);

            if (organizationUser != null && organizationUser.FirstOrDefault() != null)
            {
                profileCompanyID = organizationUser.FirstOrDefault().CompanyID;
            }

            //profileCompanyID = (organizationUser != null) ? organizationUser.Select(x => x.CompanyID).FirstOrDefault() : 0;

            IQueryable<Contract> items = PromptContractItems(profile);

            //    //沒有查詢條件預設:0000=0
            //    //是登入者未印:0011=3
            //    //是登入者未簽:0101=5   
            //    //非登入者未印:0010=2
            //    //非登入者未簽:0100=4    

            //    //StepEnum.Sealing,2
            //    //StepEnum.Sealed,3
            //    //StepEnum.DigitalSigning,4
            //    //StepEnum.DigitalSigned 5

            var contractSignatureRequestItems = items
                .SelectMany(x => x.ContractSignatureRequest)
                //判斷是查詢登入者的合約, 或是其他人的合約
                .Where(y => (Convert.ToBoolean(viewModel.ContractQueryStep & (int)QueryStepEnum.CurrentUser)) ?
                    (y.CompanyID == profileCompanyID) : //登入者的合約
                    (y.CompanyID != profileCompanyID))  //其他人的合約
                ;

            //待簽
            //若本人已用印, 但要等對方完成用印才能簽, 現行會顯示此筆, 但無法簽署
            if ((Convert.ToBoolean(viewModel.ContractQueryStep & (int)QueryStepEnum.UnSigned)))
            {
                contractSignatureRequestItems = contractSignatureRequestItems
                        .Where(x => (x.SignatureDate == null)&&(x.StampDate!=null));
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
            //var userOrg = userprofile?.OrganizationUser.Organization ?? null;

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


        public async Task<ActionResult> VueListToStampAsync([FromBody] SignContractViewModel viewModel)
        {
            ViewResult result = (ViewResult)(await ListToStampAsync(viewModel));
            result.ViewName = "~/Views/ContractConsole/VueModule/ContractRequestList.cshtml";
            return result;
        }

        public IActionResult VueApplyContract([FromBody] SignContractViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;

            if (viewModel.KeyID != null)
            {
                viewModel.ContractID = viewModel.DecryptKeyValue();
            }

            var item = models.GetTable<Contract>()
                                .Where(c => c.ContractID == viewModel.ContractID)
                                .FirstOrDefault();

            if (item == null)
            {
                return Json(new { result = false, message = "合約資料錯誤!!" });
            }

            viewModel.ContractNo = viewModel.ContractNo.GetEfficientString();
            if (viewModel.ContractNo == null)
            {
                ModelState.AddModelError("ContractNo", "請輸入合約編號!!");
            }

            viewModel.Title = viewModel.Title.GetEfficientString();
            if (viewModel.Title == null)
            {
                ModelState.AddModelError("Title", "請輸入合約編號!!");
            }

            if (!ModelState.IsValid)
            {
                return Json(new { result = false, message = ModelState.ErrorMessage() });
            }

            item.ContractNo = viewModel.ContractNo;
            item.Title = viewModel.Title;
            //item.IsJointContracting = viewModel.IsJointContracting;
            //新增簽署對象
            models.SubmitChanges();

            return Json(new { result = true });
        }

        public async Task<ActionResult> InquireDataAsync([FromBody] ContractQueryViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;

            var profile = await HttpContext.GetUserAsync();
            IQueryable<Contract> items = PromptContractItems(profile);

            viewModel.ContractNo = viewModel.ContractNo.GetEfficientString();
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

            if (viewModel.Initiator != null)
            {
                //viewModel.InitiatorID = viewModel.Initiator.DecryptKeyValue();
                var parties = models.GetTable<ContractingParty>()
                                .Where(p => p.CompanyID == viewModel.InitiatorID)
                                .Where(p => p.IsInitiator == true);
                items = items.Where(c => parties.Any(p => p.ContractID == c.ContractID));
            }

            if (viewModel.Contractor != null)
            {
                var parties = models.GetTable<ContractingParty>()
                                .Where(p => p.CompanyID == viewModel.ContractorID)
                                .Where(p => !p.IsInitiator.HasValue || p.IsInitiator == false);
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

            return View("~/Views/ContractConsole/VueModule/ContractRequestList.cshtml", items);
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
                items = items.Where(c => models.GetTable<ContractingParty>()
                            .Where(p => models.GetTable<Organization>()
                                    .Where(o => models.GetTable<OrganizationUser>()
                                            .Where(u => u.UID == profile.UID)
                                        .Any(u => u.CompanyID == o.CompanyID))
                                .Any(o => o.CompanyID == p.CompanyID))
                        .Any(p => p.ContractID == c.ContractID));
            }

            return items;
        }

        public async Task<ActionResult> ListToStampIndexAsync(SignContractViewModel viewModel)
        {
            ViewResult result = (ViewResult)(await ListToStampAsync(viewModel));
            result.ViewName = "~/Views/ContractConsole/ListToStampIndex.cshtml";
            return result;
        }

        public async Task<ActionResult> ShowCurrentContractAsync(SignContractViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;
            viewModel.KeyID = viewModel.KeyID.GetEfficientString();
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

                contract.CDS_Document.DocumentProcessLog.Add(new DocumentProcessLog
                {
                    LogDate = DateTime.Now,
                    ActorID = profile!.UID,
                    StepID = (int)CDS_Document.StepEnum.Browsed,
                });

                models.SubmitChanges();

                if (viewModel.ResultMode == DataResultMode.Download)
                {
                    Response.Headers.Add("Content-Disposition", String.Format("attachment;filename={0}.pdf", HttpUtility.UrlEncode(contract.ContractNo)));
                }

                if (contract.CDS_Document.IsPDF)
                {
                    using (MemoryStream output = contract.BuildContractWithSignature(models, viewModel.Preview == true))
                    {
                        await Response.Body.WriteAsync(output.ToArray());
                    }
                }
                else
                {
                    using (MemoryStream output = contract.BuildCurrentContract(models, viewModel.Preview == true))
                    {
                        await Response.Body.WriteAsync(output.ToArray());
                    }
                }
                await Response.Body.FlushAsync();
            }

            return new EmptyResult { };
        }

        public ActionResult PreviewCurrentContract(SignContractViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;
            return View("~/Views/ContractConsole/Module/PreviewCurrentContract.cshtml");
        }

        public ActionResult LoadContractPage(SignContractViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;
            viewModel.KeyID = viewModel.KeyID.GetEfficientString();
            if (viewModel.KeyID != null)
            {
                viewModel.ContractID = viewModel.DecryptKeyValue();
            }

            var contract = models.GetTable<Contract>()
                            .Where(c => c.ContractID == viewModel.ContractID)
                            .FirstOrDefault();

            if (contract != null)
            {
                String? imgFile = contract.GetContractImage(viewModel.PageIndex ?? 0);
                if (imgFile != null)
                {
                    var img = Bitmap.FromFile(imgFile);
                    return Json(new
                    {
                        width = img.Width,
                        height = img.Height,
                        backgroundImage = $"url('../{imgFile!.Substring(imgFile.IndexOf("logs")).Replace('\\', '/')}')",
                    });
                }
            }

            return Json(null);
        }

        public ActionResult AffixSeal(SealRequestViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;
            SealRequestViewModel tmpModel = viewModel;
            viewModel.KeyID = viewModel.KeyID.GetEfficientString();
            if (viewModel.KeyID != null)
            {
                tmpModel = JsonConvert.DeserializeObject<SealRequestViewModel>(viewModel.KeyID.DecryptData()) ?? new SealRequestViewModel { };
            }

            var item = models.GetTable<ContractSealRequest>()
                        .Where(r => r.ContractID == tmpModel.ContractID)
                        .Where(r => r.SealID == tmpModel.SealID)
                        .FirstOrDefault();

            if (item == null)
            {
                return new NotFoundResult();
            }

            return View("~/Views/ContractConsole/AffixSeal.cshtml", item);

        }

        public async Task<ActionResult> StartSigningAsync(SignatureRequestViewModel viewModel)
        {
            var result = await LoadSignatureRequestAsync(viewModel);

            ContractSignatureRequest? item = ViewBag.SignatureRequest as ContractSignatureRequest;

            if (item == null)
            {
                return new NotFoundResult();
            }

            return View("~/Views/ContractConsole/StartSigning.cshtml", item);

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

            if (jwtTokenObj.EmailTemplate==EmailBody.EmailTemplate.NotifySeal)
            {
                return await AffixPdfSeal(new SignatureRequestViewModel() { IsTrust =true,KeyID = jwtTokenObj.ContractID });
            }

            if (jwtTokenObj.EmailTemplate == EmailBody.EmailTemplate.NotifySign)
            {

                _contractServices.SetModels(models);
                ( resp, Contract contract, userProfile) =
                     _contractServices.CanPdfDigitalSign(contractID: jwtTokenObj.ContractID.DecryptKeyValue());

                if (resp.HasError)
                {
                    resp.Url = $"{Settings.Default.WebAppDomain}";
                    return View("~/Views/Shared/CustomMessage.cshtml", resp);
                }

                DigitalSignModal digitalSignModal = new DigitalSignModal()
                {

                    ContractNo = contract.ContractNo,
                    ContractTitle = contract.Title,
                    CompanyName = userProfile.CompanyName,
                    ContractID = jwtTokenObj.ContractID
                };


                return View("~/Views/Shared/DigitalSignModal.cshtml", digitalSignModal);

            }

            this.HttpContext.Logout();
            return RedirectToAction("Login", "Account");
        }


        public async Task<ActionResult> AffixPdfSeal(SignatureRequestViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;

            int? contractID = viewModel.ContractID;
            if (viewModel.KeyID != null)
            {
                contractID = viewModel.DecryptKeyValue();
            }

            _contractServices.SetModels(models);
            (BaseResponse resp, Contract contract, UserProfile userProfile) = 
                 _contractServices.CanPdfSeal(contractID: contractID);

            if (resp.HasError)
            {
                resp.Url = $"{Settings.Default.ContractListUrl}";
                if (viewModel.IsTrust!=null&&viewModel.IsTrust==true)
                {
                    resp.Url = $"{Settings.Default.WebAppDomain}";
                }
                return View("~/Views/Shared/CustomMessage.cshtml", resp);
            }

            return View("~/Views/ContractConsole/AffixPdfSealImage.cshtml", contract);

        }

        public async Task<ActionResult> CommitSignatureAsync(SealRequestViewModel viewModel, IFormFile sealImage)
        {
            if (sealImage == null)
            {
                return Json(new { result = false, message = "請選擇印鑑章!!" });
            }

            if (!(viewModel.SealScale > 0))
            {
                return Json(new { result = false, message = "請輸入正確縮放百分比!!" });
            }

            ViewResult? result = AffixSeal(viewModel) as ViewResult;
            if (result == null)
            {
                return Json(new { result = false, message = "資料錯誤!!" });
            }

            var profile = await HttpContext.GetUserAsync();

            ContractSealRequest item = (ContractSealRequest?)result?.Model ?? new ContractSealRequest { };
            if (profile != null && models.CanAffixSeal(item, profile.UID))
            {
                item.StampDate = DateTime.Now;
                item.StampUID = profile.UID;
                item.SealScale = viewModel.SealScale;
                using (MemoryStream ms = new MemoryStream())
                {
                    sealImage.CopyTo(ms);
                    item.SealImage = new System.Data.Linq.Binary(ms.ToArray());
                }
                models.SubmitChanges();

                Contract contract = item.Contract;
                if (contract.ContractSealRequest.Any(s => !s.StampDate.HasValue) == false)
                {
                    contract.ContractSignatureRequest = new EntitySet<ContractSignatureRequest>();
                    contract.ContractSignatureRequest.AddRange(contract.ContractingParty
                        .GroupBy(c => new { c.ContractID, c.CompanyID })
                        .Select(g => new ContractSignatureRequest
                        {
                            CompanyID = g.Key.CompanyID,
                            ContractID = g.Key.ContractID,
                        }));

                    models.SubmitChanges();
                }

                return Json(new { result = true });
            }

            return Json(new { result = false });

        }

        //public async Task<ActionResult> CommitPdfSignatureAsync(SignatureRequestViewModel viewModel, IFormFile sealImage)
        //{
        //    if (sealImage == null)
        //    {
        //        return Json(new { result = false, message = "請選擇印鑑章!!" });
        //    }

        //    if (!(viewModel.SealScale > 0))
        //    {
        //        return Json(new { result = false, message = "請輸入正確縮放百分比!!" });
        //    }

        //    if (!(viewModel.PageIndex >= 0))
        //    {
        //        return Json(new { result = false, message = "請選擇用印頁碼!!" });
        //    }

        //    if (!(viewModel.MarginLeft >= 0))
        //    {
        //        return Json(new { result = false, message = "請設定左邊界位置!!" });
        //    }

        //    if (!(viewModel.MarginTop >= 0))
        //    {
        //        return Json(new { result = false, message = "請設定上邊界位置!!" });
        //    }

        //    ViewResult? result = AffixPdfSeal(viewModel) as ViewResult;
        //    if (result == null)
        //    {
        //        return Json(new { result = false, message = "資料錯誤!!" });
        //    }

        //    var profile = await HttpContext.GetUserAsync();

        //    ContractSignatureRequest item = (ContractSignatureRequest)result.Model!;
        //    if (profile != null && models.CanAffixSeal(item, profile.UID))
        //    {
        //        if (viewModel.Preview != true)
        //        {
        //            item.StampDate = DateTime.Now;
        //        }
        //        //item.SignerID = profile.UID;
        //        item.SealScale = viewModel.SealScale;
        //        item.PageIndex = viewModel.PageIndex;
        //        item.MarginLeft = viewModel.MarginLeft;
        //        item.MarginTop = viewModel.MarginTop;
        //        using (MemoryStream ms = new MemoryStream())
        //        {
        //            sealImage.CopyTo(ms);
        //            item.SealImage = new System.Data.Linq.Binary(ms.ToArray());
        //        }
        //        models.SubmitChanges();

        //        return Json(new { result = true });
        //    }

        //    return Json(new { result = false });

        //}

        public async Task<ActionResult> CommitPdfNoteAsync(SignatureRequestViewModel viewModel)
        {
            viewModel.Note = viewModel.Note?.GetEfficientString();
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

            ViewResult? result = await AffixPdfSeal(viewModel) as ViewResult;
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

            ViewResult? result = await AffixPdfSeal(viewModel) as ViewResult;
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

        public async Task<ActionResult> ResetPdfSignatureAsync(SignatureRequestViewModel viewModel)
        {
            ViewResult? result = await AffixPdfSeal(viewModel) as ViewResult;
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

        public async Task<ActionResult> RptSignatureListAsync([FromBody] PostRptSignatureListRequest req)
        {

            var filters = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(req.CompanyID)
                &&(GeneralValidator.TryDecryptKeyValue(req.CompanyID)))
            {
                filters.Add("CompanyID", req.CompanyID.DecryptKeyValue());
            }

            var items = models.GetTable<ContractSignatureRequest>()
                //分頁處理-->先pass,直接轉excel
                .Where(x => x.SignatureDate != null)
                .AsQueryable<ContractSignatureRequest>()
                .EqualMultiple(filters)
                .Between("SignatureDate"
                    , string.IsNullOrEmpty(req.QueryDateEndString)?DateTime.Now.AddDays(-90).StartOfDay() : req.QueryDateFromString.ConvertToDateTime("yyyy/MM/dd").StartOfDay()
                    , string.IsNullOrEmpty(req.QueryDateEndString)?DateTime.Now.StartOfDay() : req.QueryDateEndString.ConvertToDateTime("yyyy/MM/dd").EndOfDay())
                .OrderByMultiple(new List<OrderByCol>()
                {
                    new OrderByCol(){
                    colName = nameof(ContractSignatureRequest.SignatureDate),
                    sortType = OrderbyType.Desc}
                })
                .Select(x => new
                {
                    createCompany = x.Contract.CompanyID,
                    companyName = $"{x.Organization.CompanyName}({x.Organization.CompanyID})",
                    date = x.SignatureDate,
                    signerUID = x.SignerID,
                    contractID = x.Contract.ContractID,
                    contractNoTitle = $"{x.Contract.ContractNo}-{x.Contract.Title}",
                    ContractStatus = x.Contract.CDS_Document.CurrentStep!.Value //直接套CDS_Document.StepNamin會報錯
                }).AsEnumerable()
                .Join(models.GetTable<Organization>(),
                    c => c.createCompany, o => o.CompanyID, (c, o) => (c, o))
                .Join(models.GetTable<UserProfile>(),
                    a => a.c.signerUID, u => u.UID, (a, u) => (a,u))
                .Select(x => new {
                    date = string.Format("{0:yyyy/MM/dd HH:mm:dd}", x.a.c.date!),
                    companyName = x.a.c.companyName,
                    signer = x.u.UserName,
                    createCompany = $"{x.a.o.CompanyName}({x.a.o.CompanyID})",
                    contractID = x.a.c.contractID,
                    contractNoTitle = x.a.c.contractNoTitle,
                    contractStatus = CDS_Document.StepNaming[x.a.c.ContractStatus],
                });


            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (XLWorkbook xls = new XLWorkbook())
                {
                    var abc = items.ConvertToExcel(xls, new string[7] { "簽章日期", "簽署方", "簽署人員", "起約方", "文件系統編號", "文件名稱", "文件狀態" });
                    abc.SaveAs(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    return this.File(memoryStream.ToArray(), "application/vnd.ms-excel", $"RptSignatureList-{Guid.NewGuid().ToString()}.xlsx");
                }
            }
        }

        //2024.05.29 iris:用印畫面的[退回合約], 和[TerminateContractAsync]結果一樣, 更新文件狀態為[CDS_Document.StepEnum.Revoked], 暫改用印畫面的[退回合約]為[終止文件]
        public async Task<ActionResult> AbortContractAsync(SignatureRequestViewModel viewModel)
        {
            ViewResult? result = await AffixPdfSeal(viewModel) as ViewResult;
            if (result == null)
            {
                return Json(new { result = false, message = "資料錯誤!!" });
            }

            var profile = await HttpContext.GetUserAsync();

            if (profile != null)
            {
                Contract contract = (Contract)result.Model!;
                contract.CDS_Document.TransitStep(models, profile.UID, CDS_Document.StepEnum.Revoked);
                return Json(new { result = true });
            }

            return Json(new { result = false });

        }

        public async Task<ActionResult> DeleteContract(SignatureRequestViewModel viewModel)
        {
            ViewResult? result = await AffixPdfSeal(viewModel) as ViewResult;
            if (result == null)
            {
                return Json(new { result = false, message = "資料錯誤!!" });
            }

            Contract contract = (Contract)result.Model!;
            models.DeleteAny<CDS_Document>(d => d.DocID == contract.ContractID);

            return Json(new { result = true });

        }

        public async Task<ActionResult> CommitDigitalSignatureAsync(SignatureRequestViewModel viewModel)
        {
            var result = await LoadSignatureRequestAsync(viewModel);
            Contract? contract = ViewBag.Contract as Contract;
            ContractSignatureRequest? item = ViewBag.SignatureRequest as ContractSignatureRequest;

            if (item == null)
            {
                return result;
            }

            #region 測試後再打開
            //if (contract.InProgress ?? false)
            //{
            //    return Json(new { result = false });
            //}

            //contract.InProgress = true;
            //models.SubmitChanges();
            #endregion
            
            UserProfile profile = (UserProfile)ViewBag.Profile;

            if (!item.SignerID.HasValue)
            {
                bool isSigned = false;
                if (item.Organization.DigitalSignBy() == DigitalSignCerts.Enterprise)
                {
                    isSigned = models.CHT_SignPdfByEnterprise(item, profile);
                }
                else if (item.Organization.DigitalSignBy() == DigitalSignCerts.UXB2B)
                {
                    isSigned = models.SignPdfByLocalUser(item, profile);
                }
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
                    var tid = ((String)content["tid"]).GetEfficientString();
                    if (tid != null)
                    {
                        item.RequestTicket = tid;
                        models.SubmitChanges();

                        return View("~/Views/ContractConsole/PrepareCHTSigning.cshtml", content);
                    }
                    else
                    {
                        var discountCode = ((String)content["discountCode"]).GetEfficientString();
                        if (discountCode != null)
                        {
                            content = profile.CHT_RequireIssue(discountCode);
                            if ((int?)content["result"] == 1)
                            {
                                return View("~/Views/ContractConsole/PromptToAcquireCertificate.cshtml", content);
                            }
                        }
                        else
                        {
                            return Json(new BaseResponse(haserror: true, error: $"錯誤代碼: {content["result"]}"));
                        }
                    }
                }
                //wait to do...CommitUserSignatureAsync if (!item.SignerID.HasValue), 動作一樣
                if (isSigned)
                {
                    item.Contract.ContractSignature = new ContractSignature
                    {
                        ContractSignatureRequest = item,
                    };

                    item.SignerID = profile.UID;
                    item.SignatureDate = DateTime.Now;

                    models.SubmitChanges();

                    item.Contract.CDS_Document.AddDocumentProcessLog(models, profile!.UID, CDS_Document.StepEnum.DigitalSigned);
                    item.Contract.CDS_Document.UpdateCurrentStep(models, CDS_Document.StepEnum.DigitalSigning);

                    if (item.Contract.isAllDigitalSignatureDone())
                    {
                        item.Contract.CDS_Document.UpdateCurrentStep(models, CDS_Document.StepEnum.DigitalSigned);
                    }

                    if (!models.GetTable<ContractSignatureRequest>()
                        .Where(c => c.ContractID == item.ContractID)
                        .Where(c => !c.SignerID.HasValue)
                        .Any())
                    {
                        item.Contract.CDS_Document.TransitStep(models, profile!.UID, CDS_Document.StepEnum.Committed);
                        _contractServices?.SetModels(models);
                        _contractServices?.SendContractNotifyEmailAsync(item?.Contract, EmailBody.EmailTemplate.FinishContract);
                    }

                    if (contract.InProgress != null && contract.InProgress == true)
                    {
                        contract.InProgress = null;
                        models.SubmitChanges();
                    }

                    return Json(baseResponse);

                }
            } 
            else
            {
                return Json(new BaseResponse(haserror: true, error: "合約已有簽署記錄, 無法再次簽署."));
            }

            if (viewModel.IsTrust != null && viewModel.IsTrust == true)
            {
                baseResponse.Url = $"{Settings.Default.WebAppDomain}";
                return View("~/Views/Shared/CustomMessage.cshtml", baseResponse);
            }

            return Json(baseResponse);

        }

        //2024.05.29 iris:查詢畫面的[終止文件], 和[AbortContractAsync]結果一樣, 更新文件狀態為[CDS_Document.StepEnum.Revoked]
        public async Task<ActionResult> TerminateContractAsync(SignatureRequestViewModel viewModel)
        {
            var result = LoadContract(viewModel);
            Contract? item = ViewBag.Contract as Contract;

            if (item == null)
            {
                return result;
            }

            if (item.CDS_Document.CurrentStep == (int)CDS_Document.StepEnum.Terminated)
            {
                return Json(new { result = true });
            }

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
                    item.CDS_Document.TransitStep(models, profile.UID, CDS_Document.StepEnum.Revoked);
                    return Json(new { result = true });
                }
            }
            else
            {
                if (profile?.OrganizationUser == null)
                {
                    return Json(new { result = false, message = "合約簽署人資料錯誤!!" });
                }

                if (!models.GetTable<ContractingParty>()
                        .Where(p => p.ContractID == item.ContractID)
                        .Where(p => p.CompanyID == profile.OrganizationUser.CompanyID)
                        .Any())
                {
                    return Json(new { result = false, message = "合約簽署人資料錯誤!!" });
                }

                item.CDS_Document.TransitStep(models, profile.UID, CDS_Document.StepEnum.Terminated);
                return Json(new { result = true });
            }


        }

        public async Task<ActionResult> CommitUserSignatureAsync(SignatureRequestViewModel viewModel)
        {
            var result = await LoadSignatureRequestAsync(viewModel);

            ContractSignatureRequest? item = ViewBag.SignatureRequest as ContractSignatureRequest;

            if (item == null)
            {
                return result;
            }

            UserProfile profile = (UserProfile)ViewBag.Profile;
            //wait to do...合併CommitDigitalSignatureAsync if(Signed), 動作一樣
            if (!item.SignerID.HasValue)
            {
                (bool signOk, string code) = models.CHT_SignPdfByUser(item, profile);
                if (signOk)
                {
                    item.Contract.ContractSignature = new ContractSignature
                    {
                        ContractSignatureRequest = item,
                    };

                    item.SignerID = profile.UID;
                    item.SignatureDate = DateTime.Now;

                    models.SubmitChanges();

                    item.Contract.CDS_Document.AddDocumentProcessLog(models, profile!.UID, CDS_Document.StepEnum.DigitalSigned);
                    item.Contract.CDS_Document.UpdateCurrentStep(models, CDS_Document.StepEnum.DigitalSigning);

                    if (item.Contract.isAllDigitalSignatureDone())
                    {
                        item.Contract.CDS_Document.UpdateCurrentStep(models, CDS_Document.StepEnum.DigitalSigned);
                    }

                    if (!models.GetTable<ContractSignatureRequest>()
                        .Where(c => c.ContractID == item.ContractID)
                        .Where(c => !c.SignerID.HasValue)
                        .Any())
                    {
                        item.Contract.CDS_Document.TransitStep(models, profile!.UID, CDS_Document.StepEnum.Committed);
                        _contractServices?.SetModels(models);
                        _contractServices?.SendContractNotifyEmailAsync(item?.Contract, EmailBody.EmailTemplate.FinishContract);
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

        //public async Task<ActionResult> ShowCurrentContractByWordAsync(SignContractViewModel viewModel)
        //{
        //    ViewBag.ViewModel = viewModel;
        //    viewModel.KeyID = viewModel.KeyID.GetEfficientString();
        //    if (viewModel.KeyID != null)
        //    {
        //        viewModel.ContractID = viewModel.DecryptKeyValue();
        //    }

        //    var contract = models.GetTable<Contract>()
        //                    .Where(c => c.ContractID == viewModel.ContractID)
        //                    .FirstOrDefault();

        //    if (contract != null)
        //    {
        //        var pdfFile = contract.BuildCurrentContractByWord(models);
        //        if (pdfFile != null)
        //        {
        //            return PhysicalFile(pdfFile, "application/pdf");
        //        }
        //    }

        //    return new EmptyResult { };

        //}

        [AllowAnonymous]
        public async Task<ActionResult> GetSignerSealAsync(SignatureRequestViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;
            SignatureRequestViewModel tmpModel = viewModel;
            viewModel.KeyID = viewModel.KeyID.GetEfficientString();
            if (viewModel.KeyID != null)
            {
                tmpModel = JsonConvert.DeserializeObject<SignatureRequestViewModel>(viewModel.KeyID.DecryptData()) ?? new SignatureRequestViewModel { };
            }

            var item = models.GetTable<ContractSignatureRequest>()
                        .Where(r => r.ContractID == tmpModel.ContractID)
                        .Where(r => r.CompanyID == tmpModel.CompanyID)
                        .FirstOrDefault();

            if (item?.SealImage != null)
            {
                Response.Clear();
                Response.ContentType = "application/octet-stream";
                Response.Body.Write(item.SealImage.ToArray());

                await Response.Body.FlushAsync();
            }

            return new EmptyResult();

        }

        [AllowAnonymous]
        public async Task<ActionResult> NotifyClientCertificateAsync(SignatureRequestViewModel viewModel)
        {
            await DumpAsync();
            return Content("");
        }

        [RequestSizeLimit(200 * 1024 * 1024)]
        public async Task<IActionResult> InitialContract(IFormFile file)
        {
            var profile = (await HttpContext.GetUserAsync()).LoadInstance(models);
            if (profile == null || profile.OrganizationUser == null)
            {
                return Json(new { result = false, message = "請重新登入" });
            }
            if (file == null)
            {
                return Json(new { result = false, message = "請選擇檔案!!" });
            }

            String extName = Path.GetExtension(file.FileName).ToLower();
            if (extName != ".pdf")
            {
                return Json(new { result = false, message = "合約檔案類型只能是pdf!!" });
            }

            Contract contract = new Contract
            {
                FilePath = file.StoreContractDocument(),
                CompanyID = profile.OrganizationUser.CompanyID,
                ContractNo = String.Empty,
                CDS_Document = new CDS_Document
                {
                    DocDate = DateTime.Now,
                    CurrentStep = (int)CDS_Document.StepEnum.Initial,
                    ProcessType = (int)CDS_Document.ProcessTypeEnum.PDF,
                },
            };

            models.GetTable<Contract>().InsertOnSubmit(contract);
            try
            {
                models.SubmitChanges();
                return Json(new { result = true, message = contract.ContractID.EncryptKey() });
            }
            catch (Exception ex)
            {
                FileLogger.Logger.Error(ex);
                return Json(new { result = false, message = ex.Message });
            }

        }

        [HttpPost]
        //是否有Contract產製修改權限ContractHome.Security.Authorization
        public async Task<ActionResult> ConfigAsync([FromBody] PostConfigRequest req)
        {
            var profile = await HttpContext.GetUserAsync();
            #region add for postman test
            if (profile == null && req.EncUID!=null)
            {
                profile = models.GetTable<UserProfile>().Where(x => x.UID == req.EncUID.DecryptKeyValue()).FirstOrDefault();
            }
            #endregion
            try
            {
                _contractServices.SetModels(models);
                var contractID = req.ContractID.ToString().DecryptKeyValue();
                Contract contract = _contractServices.GetContractByID(contractID: contractID);
                if (contract == null)
                {
                    ModelState.AddModelError("", "合約不存在");
                    return BadRequest();
                }
                //wait to do...獨立控管每項作業可執行step
                if (contract.CDS_Document.CurrentStep >= 2)
                {
                    ModelState.AddModelError("", "合約已進行中,無法修改資料");
                    return BadRequest();
                }
                _contractServices.SetConfig(contract, req);
                contract.CDS_Document.TransitStepTest(models, profile!.UID, CDS_Document.StepEnum.Config, 
                    ClientIP: _contractServices.GetClientIP, 
                    ClientDevice: _contractServices.GetClientDevice);
                models.SubmitChanges();
                return Content(baseResponse.JsonStringify());
            }
            catch (Exception ex)
            {
                FileLogger.Logger.Error($"{req.ToString()} {ex.ToString()}");
                return Content(new BaseResponse(true, "").JsonStringify());
            }

        }

        [HttpPost]
        public async Task<IActionResult> AvailableSignatoriesAsync([FromBody] GetSignatoriesRequest req)
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

            IEnumerable <Organization> signatories
                = _contractServices.GetAvailableSignatories(contract.CompanyID);

            baseResponse.Data = signatories.Select(x =>
                new
                {
                    id = x.CompanyID.EncryptKey(),
                    companyName = x.CompanyName,
                    receiptNo = x.ReceiptNo
                });

            return Json(baseResponse);
        }

        [HttpPost]
        public async Task<IActionResult> FeildSettingsAsync([FromBody] GetFieldSettingRequest req)
        {
            _contractServices.SetModels(models);
            Contract contract = _contractServices.GetContractByID(contractID: req.ContractID.DecryptKeyValue());

            baseResponse.Data =
                contract
                .ContractSignaturePositionRequest
                .Where(y => y.ContractorID == req.CompanyID.DecryptKeyValue())
                .Select(x => new ContractSignaturePositionRequest()
                {
                    RequestID = x.RequestID,
                    CompanyID = x.ContractorID.EncryptKey(),
                    PositionID = x.PositionID,
                    ScaleWidth = x.ScaleWidth,
                    ScaleHeight = x.ScaleHeight,
                    MarginTop = x.MarginTop,
                    MarginLeft = x.MarginLeft,
                    Type = x.Type,
                    PageIndex = x.PageIndex
                });

            return Json(baseResponse);
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
            //wait to do..contract business 物件
            foreach (var tt in req.FieldSettings)
            {
                if (!_contractServices.IsContractHasCompany(contract: contract, companyID: tt.CompanyID.DecryptKeyValue()))
                    return Json(new BaseResponse(false, $"{tt.CompanyID.DecryptKeyValue()} not belonged."));
            }
            //wait to do..檢查currentStep是否可進行UpdateFieldSetting
            //contract.CDS_Document.CheckIfCanGo
            _contractServices.UpdateFieldSetting(contract, req.FieldSettings);
            models.SubmitChanges();
            contract.CDS_Document.TransitStep(models, profile!.UID, CDS_Document.StepEnum.FieldSet);
            models.SubmitChanges();
            return Json(baseResponse);
        }

        [HttpPost]
        public async Task<ActionResult> EstablishAsync([FromBody] GetSignatoriesRequest req)
        {
            var profile = await HttpContext.GetUserAsync();
            #region add for postman test
            if (profile == null&& req.EncUID.Length>0)
            {
                profile = models.GetTable<UserProfile>().Where(x => x.UID == 4).FirstOrDefault();
            }
            #endregion
            _contractServices?.SetModels(models: models);
            var contract = _contractServices?.GetContractByID(req.ContractID.DecryptKeyValue());
            //wait to do...獨立控管每項作業可執行step
            if (contract.CDS_Document.CurrentStep >= 5)
            {
                return Json(new BaseResponse(true, "合約已進行中,無法修改資料"));
            }
            if (contract?.FilePath == null || !System.IO.File.Exists(contract.FilePath))
            {
                return Json(new BaseResponse(true, "合約資料錯誤!!"));
            }
            contract.ContractContent = new Binary(System.IO.File.ReadAllBytesAsync(contract.FilePath).Result);
            contract.CDS_Document.TransitStep(models, profile!.UID, CDS_Document.StepEnum.Establish);
            
            if (contract.IsPassStamp==true)
            {
                contract.CDS_Document.TransitStep(models, profile!.UID, CDS_Document.StepEnum.Sealed);
            }

            models.SubmitChanges();

            //2.如果是大量發送, 複製合約
            //var newContract = _contractServices.CreateAndSaveContractByOld(contract);

            //_contractServices.CreateAndSaveParty(
            //    initiatorID: initiatorID,
            //    contractorID: contractorID ?? 0,
            //    contract: newContract,
            //    viewModel.Contractors[i].SignaturePositions,
            //    uid ?? 0
            //);

            //3.發送通知(one by one)
            _contractServices?.SendContractNotifyEmailAsync(contract,
                        (contract.IsPassStamp == true) ?
                            EmailBody.EmailTemplate.NotifySign :
                            EmailBody.EmailTemplate.NotifySeal);

            return Json(baseResponse);
        }

        public class ContractSignaturePositionRequest
        {
            public string CompanyID { get; set; }
            public string PositionID { get; set; }
            public int RequestID { get; set; }
            public double? ScaleWidth { get; set; }
            public double? ScaleHeight { get; set; }
            public double? MarginTop { get; set; }
            public double? MarginLeft { get; set; }
            public int? PageIndex { get; set; }
            //0:default 1:文字 2.地址 3.電話 4.日期 5.公司Title 6.印章 7.簽名 8.圖片 ... 擴充?
            public int? Type { get; set; }
        }
    }
}