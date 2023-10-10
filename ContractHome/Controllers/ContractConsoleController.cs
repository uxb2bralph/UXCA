using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO;
using ContractHome.Models;
using ContractHome.Models.DataEntity;
using ContractHome.Models.ViewModel;
using CommonLib.Utility;
using Newtonsoft.Json;
using ContractHome.Helper;
using ContractHome.Properties;
using CommonLib.Core.Utility;
using System.Xml;
using GemBox.Document;
using System.Net;
using Microsoft.Extensions.Primitives;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.AspNetCore.WebUtilities;
using System.Linq.Dynamic.Core;
using ContractHome.Models.Helper;
using System.Data.Linq;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Data;
using Newtonsoft.Json.Linq;
using ContractHome.Helper.DataQuery;
using Azure;
using DocumentFormat.OpenXml.Drawing.Charts;
using System.Web;

namespace ContractHome.Controllers
{
    [Authorize]
    public class ContractConsoleController : SampleController
    {
        private readonly ILogger<HomeController> _logger;

        public ContractConsoleController(ILogger<HomeController> logger, IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _logger = logger;
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

            if(viewModel.Initiator!=null)
            {
                viewModel.InitiatorID = viewModel.Initiator.DecryptKeyValue();
            }
            if (!viewModel.InitiatorID.HasValue)
            {
                ModelState.AddModelError("Initiator", "請選擇合約發起人!!");
            }

            if (viewModel.Contractor != null)
            {
                viewModel.ContractorID = viewModel.Contractor.DecryptKeyValue();
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

        public async Task<ActionResult> CommitContractAsync([FromBody]SignContractViewModel viewModel)
        {
            if (viewModel.Initiator != null)
            {
                viewModel.InitiatorID = viewModel.Initiator.DecryptKeyValue();
            }
            if (!viewModel.InitiatorID.HasValue)
            {
                ModelState.AddModelError("Initiator", "請選擇合約發起人!!");
            }

            if (!(viewModel.MultiContractor?.Length >0))
            {
                ModelState.AddModelError("Contractor", "請選擇簽約人!!");
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

            var profile = await HttpContext.GetUserAsync();

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

            if (!contract.ContractSealRequest.Any() && viewModel.IgnoreSeal != true)
            {
                return Json(new { result = false, message = "合約未用印!!" });
            }

            contract.ContractContent = new Binary(System.IO.File.ReadAllBytes(contract.FilePath));

            void makeEffective(Contract c,int initiatorID,int contractorID)
            {
                if(!c.ContractingParty.Where(p=>p.CompanyID==initiatorID)
                    .Where(p => p.IntentID == (int)ContractingIntent.ContractingIntentEnum.Initiator)
                    .Any())
                {
                    c.ContractingParty.Add(new ContractingParty
                    {
                        CompanyID = initiatorID,
                        IntentID = (int)ContractingIntent.ContractingIntentEnum.Initiator,
                        IsInitiator = true,
                    });
                }

                if (!c.ContractingParty.Where(p => p.CompanyID == contractorID)
                                    .Where(p => p.IntentID == (int)ContractingIntent.ContractingIntentEnum.Contractor)
                                    .Any())
                {
                    c.ContractingParty.Add(new ContractingParty
                    {
                        CompanyID = contractorID,
                        IntentID = (int)ContractingIntent.ContractingIntentEnum.Contractor,
                    });
                }

                if (!c.ContractSignatureRequest.Any(r => r.CompanyID == initiatorID))
                {
                    c.ContractSignatureRequest.Add(new ContractSignatureRequest
                    {
                        CompanyID = initiatorID,
                        StampDate = DateTime.Now,
                    });
                }

                if (!c.ContractSignatureRequest.Any(r => r.CompanyID == contractorID))
                {
                    c.ContractSignatureRequest.Add(new ContractSignatureRequest
                    {
                        CompanyID = contractorID,
                    });
                }

                models.SubmitChanges();

                c.CDS_Document.TransitStep(models, profile!.UID, CDS_Document.StepEnum.InitiatorSealed);
            }


            models.SubmitChanges();

            List<Contract> contractItems = new List<Contract>
            {
                contract
            };

            if (viewModel.MultiContractor!.Length>1)
            {
                var doc = models.GetTable<CDS_Document>().Where(d => d.DocID == contract.ContractID).First();
                String json = doc.JsonStringify();
                for (int i = 1; i < viewModel.MultiContractor!.Length; i++)
                {
                    doc = JsonConvert.DeserializeObject<CDS_Document>(json);
                    models.GetTable<CDS_Document>().InsertOnSubmit(doc!);
                    models.SubmitChanges();
                    contractItems.Add(doc!.Contract);
                }
            }

            for(int i = 0;i< viewModel.MultiContractor!.Length;i++)
            {
                makeEffective(contractItems[i], viewModel.InitiatorID!.Value, viewModel.MultiContractor![i].DecryptKeyValue());
            }

            return Json(new { result = true,dataItem = new { contract.ContractNo,contract.Title } });
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

            contract.CDS_Document.TransitStep(models, profile!.UID, CDS_Document.StepEnum.ContractorSealed);

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

        public async Task<ActionResult> ListToStampAsync(SignContractViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;

            var profile = await HttpContext.GetUserAsync();
            IQueryable<Contract> items = PromptContractItems(profile);
            viewModel.RecordCount = items?.Count();

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

        public async Task<ActionResult> VueListToStampAsync([FromBody]SignContractViewModel viewModel)
        {
            ViewResult result = (ViewResult)(await ListToStampAsync(viewModel));
            result.ViewName = "~/Views/ContractConsole/VueModule/ContractRequestList.cshtml";
            return result;
        }

        public IActionResult VueApplyContract([FromBody]SignContractViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;

            if(viewModel.KeyID != null)
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

            models.SubmitChanges();

            return Json(new { result = true });
        }


        public async Task<ActionResult> InquireDataAsync([FromBody]ContractQueryViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;

            var profile = await HttpContext.GetUserAsync();
            IQueryable<Contract> items = PromptContractItems(profile);

            viewModel.ContractNo = viewModel.ContractNo.GetEfficientString();
            if (viewModel.ContractNo != null)
            {
                items = items.Where(c => c.ContractNo.StartsWith(viewModel.ContractNo));
            }

            bool queryByDate = false;
            IQueryable<CDS_Document> documents = models.GetTable<CDS_Document>();
            if (viewModel.ContractDateFrom.HasValue)
            {
                documents = documents.Where(d => d.DocDate >= viewModel.ContractDateFrom);
                queryByDate = true;
            }
            if (viewModel.ContractDateTo.HasValue)
            {
                documents = documents.Where(d => d.DocDate < viewModel.ContractDateTo.Value.AddDays(1));
                queryByDate = true;
            }
            if (queryByDate)
            {
                items = items.Where(c => documents.Any(d => d.DocID == c.ContractID));
            }

            if (viewModel.Initiator != null)
            {
                viewModel.InitiatorID = viewModel.Initiator.DecryptKeyValue();
                var parties = models.GetTable<ContractingParty>()
                                .Where(p => p.CompanyID == viewModel.InitiatorID)
                                .Where(p => p.IsInitiator == true);
                items = items.Where(c => parties.Any(p => p.ContractID == c.ContractID));
            }

            if (viewModel.Contractor != null)
            {
                viewModel.ContractorID = viewModel.Contractor.DecryptKeyValue();
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
            IQueryable<CDS_Document> docItems =
                models.GetTable<CDS_Document>()
                    .Where(d => !d.CurrentStep.HasValue || d.CurrentStep != (int)CDS_Document.StepEnum.Removed);

            IQueryable<Contract> items =
                models.GetTable<Contract>()
                    .Where(c => docItems.Any(d => d.DocID == c.ContractID));

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
                    return Json(new {
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
                return new NotFoundResult();
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
            if(result == null)
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

            if (profile != null)
            {
                Contract contract = (Contract)result.Model!;
                ContractSealRequest item = new ContractSealRequest
                {
                    ContractID = contract.ContractID,
                    SealID = seal.SealID,
                    StampDate = DateTime.Now,
                    StampUID = profile.UID,
                    SealScale = viewModel.SealScale,
                    MarginLeft = viewModel.MarginLeft,  
                    MarginTop = viewModel.MarginTop,
                    PageIndex = viewModel.PageIndex,
                };

                models.GetTable<ContractSealRequest>().InsertOnSubmit(item);
                models.SubmitChanges();

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
                                .Where(s => s.PageIndex == viewModel.PageIndex)
                                .Where(s => s.ContractID == contract.ContractID);
                table.DeleteAllOnSubmit(items);
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
                contract.CDS_Document.TransitStep(models, profile.UID, CDS_Document.StepEnum.Removed);
                return Json(new { result = true });
            }

            return Json(new { result = false });

        }

        public async Task<ActionResult> CommitDigitalSignatureAsync(SignatureRequestViewModel viewModel)
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

                    var party = models.GetTable<ContractingParty>()
                        .Where(p => p.ContractID == item.ContractID)
                        .Where(p => p.CompanyID == item.CompanyID).FirstOrDefault();

                    if (party?.IsInitiator == true)
                    {
                        item.Contract.CDS_Document.TransitStep(models, profile!.UID, CDS_Document.StepEnum.InitiatorDigitalSigned);
                    }
                    else
                    {
                        item.Contract.CDS_Document.TransitStep(models, profile!.UID, CDS_Document.StepEnum.ContractorDigitalSigned);
                    }

                    return Json(new { result = true });
                }
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
                    item.CDS_Document.TransitStep(models, profile.UID, CDS_Document.StepEnum.Removed);
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