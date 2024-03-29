﻿using Microsoft.AspNetCore.Authorization;
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
using System.ComponentModel;
using System.Linq;
using ContractHome.Models.Email.Template;
using ContractHome.Models.Email;
using System.Runtime.CompilerServices;
using static ContractHome.Models.DataEntity.CDS_Document;

namespace ContractHome.Controllers
{
    //remark for testing by postman
    [Authorize]
    public class ContractConsoleController : SampleController
    {
        private readonly ILogger<HomeController> _logger;
        private ContractServices? _contractServices;
        private readonly IMailService _mailService;
        public ContractConsoleController(ILogger<HomeController> logger,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _logger = logger;
            _contractServices = ServiceProvider.GetRequiredService<ContractServices>(); ;
            _mailService = ServiceProvider.GetRequiredService<IMailService>();
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

                        if ((contract.IsJointContracting == true) || (i == 0))
                        {
                            _contractServices?.CreateAndSaveParty(
                                initiatorID: initiatorID,
                                contractorID: contractorID ?? 0,
                                contract: contract,
                                viewModel.Contractors[i].SignaturePositions,
                                uid ?? 0);

                        }
                        else
                        {
                            var newContract = _contractServices.CreateAndSaveContractByOld(contract);

                            _contractServices.CreateAndSaveParty(
                                initiatorID: initiatorID,
                                contractorID: contractorID ?? 0,
                                contract: newContract,
                                viewModel.Contractors[i].SignaturePositions,
                                uid ?? 0
                            );
                            notifyList.Add(newContract);
                        }

                    }
                }

                _contractServices.SaveContract();

                if (notifyList.Count > 0)
                {
                    await foreach (var mailData in _contractServices.GetContractorNotifyEmailAsync(
                        notifyList, EmailBody.EmailTemplate.NotifySeal))
                    {
                        _mailService.SendMailAsync(mailData, default);
                    }
                }
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

            var sealItems = models.GetTable<SealTemplate>().Where(s => s.UID == profile.UID);
            if (!models.GetTable<ContractSealRequest>()
                .Where(s => s.ContractID == contract.ContractID)
                .Where(s => sealItems.Any(t => t.SealID == s.SealID))
                .Any())
            {
                return Json(new { result = false, message = "簽約人尚未用印!!" });
            }

            requestItem.StampDate = DateTime.Now;
            models.SubmitChanges();

            if (contract.isAllStamped())
            { 
                contract.CDS_Document.TransitStep(models, profile!.UID, CDS_Document.StepEnum.Sealed);
                _contractServices?.SetModels(models);
                await foreach (var mailData in _contractServices?.GetAllContractUsersNotifyEmailAsync(
                    new List<Contract>() { contract }, EmailBody.EmailTemplate.NotifySign))
                {
                    _mailService.SendMailAsync(mailData, default);
                }
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
            UnCommited =8  // 1000
        }

        public async Task<ActionResult> ListToStampAsync(SignContractViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;
            if (viewModel.ContractQueryStep == null) { viewModel.ContractQueryStep = 0; }

            var profile = await HttpContext.GetUserAsync();
            var profileCompanyID = 0;
            var organizationUser = models.GetTable<OrganizationUser>().Where(x => x.UID == profile.UID);
            profileCompanyID = (organizationUser!=null)?organizationUser.Select(x=>x.CompanyID).FirstOrDefault():0;

            IQueryable<Contract> items = PromptContractItems(profile);

            //IQueryable<CDS_Document> docItems =
            //    models.GetTable<CDS_Document>()
            //        .Where(d => !d.CurrentStep.HasValue || CDS_Document.PendingState.Contains((CDS_Document.StepEnum)d.CurrentStep!));

            //items = items.Where(c => docItems.Any(d => d.DocID == c.ContractID));

            items = items.Where(d => !d.CDS_Document.CurrentStep.HasValue 
                || CDS_Document.PendingState.Contains((CDS_Document.StepEnum)d.CDS_Document.CurrentStep!));

            #region 處理查詢條件:是否為登入者/是否已用印/是否已用簽
            if (viewModel.ContractQueryStep >= 2 && viewModel.ContractQueryStep <= 5)
            {

                //沒有查詢條件預設:0000=0
                //是登入者未印:0011=3
                //是登入者未簽:0101=5   
                //非登入者未印:0010=2
                //非登入者未簽:0100=4    

                //StepEnum.Sealing,2
                //StepEnum.Sealed,3
                //StepEnum.DigitalSigning,4
                //StepEnum.DigitalSigned 5

                //FileLogger.Logger.Error(String.Join(",", items.Select(x => x.ContractID)));
                List<int> removeContractID = new List<int>();

                var removeContract = items
                    .SelectMany(x => x.ContractSignatureRequest)
                    //判斷是查詢登入者的合約, 或是其他人的合約
                    .Where(y => (Convert.ToBoolean(viewModel.ContractQueryStep & (int)QueryStepEnum.CurrentUser)) ?
                        (y.CompanyID == profileCompanyID) :
                        (y.CompanyID != profileCompanyID))
                    ;

                //if StepEnum.Sealing and StampDate!=null then 已印->移除contract
                //if StepEnum.Sealing and StampDate==null then 未印->不移除contract
                if ((Convert.ToBoolean(viewModel.ContractQueryStep & (int)QueryStepEnum.UnStamped)))
                {
                    removeContract = removeContract.Where(y => y.StampDate != null);
                }

                if ((Convert.ToBoolean(viewModel.ContractQueryStep & (int)QueryStepEnum.UnSigned)))
                {
                    removeContract = removeContract
                        .Where(y => ((y.SignatureDate != null)||(y.StampDate == null)))
                        .Where(y => (y.Contract.CDS_Document.CurrentStep == (int)StepEnum.Sealing));
                }

                removeContractID = removeContract.Select(x => x.Contract.ContractID).Distinct().ToList();


                //FileLogger.Logger.Error(String.Join(",", removeContractID.Select(x => x)));
                items = items.Where(y => !removeContractID.Contains(y.ContractID));
                //FileLogger.Logger.Error(String.Join(",", items.Select(x => x.ContractID)));

            }
            #endregion
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
            item.IsJointContracting = viewModel.IsJointContracting;

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
                if (_date!=null)
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


        public ActionResult AffixPdfSeal(SignatureRequestViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;

            int? contractID = viewModel.ContractID;
            if (viewModel.KeyID != null)
            {
                contractID = viewModel.DecryptKeyValue();
            }

            var item = models.GetTable<Contract>()
                                .Where(c => c.ContractID == contractID)
                                .FirstOrDefault();

            if (item == null)
            {
                return new BadRequestResult();
            }

            return View("~/Views/ContractConsole/AffixPdfSealImage.cshtml", item);

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

            ViewResult? result = AffixPdfSeal(viewModel) as ViewResult;
            if (result == null)
            {
                return Json(new { result = false, message = "資料錯誤!!" });
            }

            var profile = await HttpContext.GetUserAsync();

            void ApplyNote(Contract contract,int? uid,int? pageIndex)
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

            ViewResult? result = AffixPdfSeal(viewModel) as ViewResult;
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
            ViewResult? result = AffixPdfSeal(viewModel) as ViewResult;
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

        public async Task<ActionResult> AbortContractAsync(SignatureRequestViewModel viewModel)
        {
            ViewResult? result = AffixPdfSeal(viewModel) as ViewResult;
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
            ViewResult? result = AffixPdfSeal(viewModel) as ViewResult;
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
            ContractSignatureRequest ? item = ViewBag.SignatureRequest as ContractSignatureRequest;

            if (item == null)
            {
                return result;
            }

            if (contract.InProgress??false)
            {
                return Json(new { result = false });
            }

            contract.InProgress = true;
            models.SubmitChanges();

            UserProfile profile = (UserProfile)ViewBag.Profile;
            if (!item.SignerID.HasValue)
            {
                bool isSigned = false;
                if (item.Organization.CHT_Token != null)
                {
                    isSigned = models.CHT_SignPdfByEnterprise(item, profile);
                }
                else if (item.Organization.OrganizationToken?.PKCS12 != null)
                {
                    isSigned = models.SignPdfByLocalUser(item, profile);
                }
                else
                {
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
                    }
                }

                if (isSigned)
                {
                    item.Contract.ContractSignature = new ContractSignature
                    {
                        ContractSignatureRequest = item,
                    };

                    item.SignerID = profile.UID;
                    item.SignatureDate = DateTime.Now;

                    models.SubmitChanges();

                    //var party = models.GetTable<ContractingParty>()
                    //    .Where(p => p.ContractID == item.ContractID)
                    //    .Where(p => p.CompanyID == item.CompanyID).FirstOrDefault();

                    //if (party?.IsInitiator == true)
                    //{
                    //    item.Contract.CDS_Document.TransitStep(models, profile!.UID, CDS_Document.StepEnum.InitiatorDigitalSigned);
                    //}
                    //else
                    //{
                    //    if (item.Contract.ifDigitalSignatureFlowFinished())
                    //    { 
                    //        item.Contract.CDS_Document.TransitStep(models, profile!.UID, CDS_Document.StepEnum.ContractorDigitalSigned);
                    //    }
                    //}

                    if (item.Contract.isAllDigitalSignatureDone())
                    {
                        item.Contract.CDS_Document.TransitStep(models, profile!.UID, CDS_Document.StepEnum.DigitalSigned);
                    } 
                    else
                    {
                        item.Contract.CDS_Document.TransitStep(models, profile!.UID, CDS_Document.StepEnum.DigitalSigning);
                    }

                    if (!models.GetTable<ContractSignatureRequest>()
                        .Where(c => c.ContractID == item.ContractID)
                        .Where(c => !c.SignerID.HasValue)
                        .Any())
                    {
                        item.Contract.CDS_Document.TransitStep(models, profile!.UID, CDS_Document.StepEnum.Committed);
                        _contractServices?.SetModels(models);
                        await foreach (var mailData in 
                            _contractServices?.GetAllContractUsersNotifyEmailAsync(
                            new List<Contract>() { item?.Contract }, EmailBody.EmailTemplate.FinishContract))
                        {
                            _mailService?.SendMailAsync(mailData, default);
                        }
                    }

                    if (contract.InProgress != null && contract.InProgress == true)
                    {
                        contract.InProgress = null;
                        models.SubmitChanges();
                    }

                    return Json(new { result = true });
                }
            }


            if (contract.InProgress != null && contract.InProgress == true)
            {
                contract.InProgress = null;
                models.SubmitChanges();
            }

            return Json(new { result = false });

        }

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
            if (!item.SignerID.HasValue)
            {
                if (models.CHT_SignPdfByUser(item, profile))
                {
                    item.Contract.ContractSignature = new ContractSignature
                    {
                        ContractSignatureRequest = item,
                    };

                    item.SignerID = profile.UID;
                    item.SignatureDate = DateTime.Now;

                    models.SubmitChanges();
                    return Json(new { result = true });
                }
            }

            return Json(new { result = false });

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

        public IActionResult InitialContract(IFormFile file)
        {
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

    }


}