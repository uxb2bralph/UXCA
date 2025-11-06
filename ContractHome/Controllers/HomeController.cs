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
using ContractHome.Models.Email;

namespace ContractHome.Controllers
{
    public class HomeController : SampleController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IMailService _mailService;


        public HomeController(ILogger<HomeController> logger, 
            IServiceProvider serviceProvider,
            IMailService _MailService) : base(serviceProvider)
        {
            _logger = logger;
            _mailService = _MailService;
        }

        public IActionResult Index()
        {
            this.HttpContext.Logout();
            return RedirectToAction("Login", "Account");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult ReloadSettings()
        {
            Settings.Reload();
            return Json(Settings.Default);
        }

        public IActionResult ApplyTemplate(String template)
        {
            if (string.IsNullOrEmpty(template)
                || template.Contains("..")
                || template.Contains('/')
                || template.Contains('\\'))
            {
                return Json(new { result = false, message = "Invalid template file name!" });
            }

            String templatePath = System.IO.Path.Combine(FileLogger.Logger.LogPath, "Template", template);
            if(System.IO.File.Exists(templatePath)) 
            {
                Settings.Default.TemplateContractDocx = template;
                Settings.Default.Save();
                return Json(new { result = true, template });
            }
            else
            {
                return Json(new { result = false, message = "範本錯誤!!" });
            }
        }

        public IActionResult StoreSettings()
        {
            Settings.Default.Save();
            return Content(Settings.Default.JsonStringify(), "application/json");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [AllowAnonymous]
        public ActionResult ApplyContract(IFormCollection forms, ErrorViewModel errorModel)
        {
            Dictionary<String, String> items = new Dictionary<string, string>()
            {
                //{ "(1)","(一)" },
                //{ "(2)","(二)" },
                //{ "(3)","(三)" },
                //{ "(4)","(四)" },
                //{ "(5)","(五)" },
                //{ "(6)","(六)" },
                //{ "(7)","(七)" },
                //{ "(8)","(八)" },
                //{ "(9)","(九)" },
                //{ "(10)","(十)" },
            };
            foreach (var nameValue in forms)
            {
                if (!items.ContainsKey(nameValue.Key))
                {
                    items.Add($"［＄{nameValue.Key}＄］", nameValue.Value);
                }
            }

            //Paragraph? pp = null;
            void ReplaceToken(Element element)
            {
                //if(element.Content.Find("用途：").Any())
                //{
                //    if(element is Paragraph) 
                //    {
                //        pp = (Paragraph)element;
                //    }
                //    System.Diagnostics.Debugger.Break();
                //}

                foreach (var item in items)
                {
                    element.Content.Replace(item.Key, item.Value);
                }
            }

            byte[] GetImage(String url)
            {
                using (WebClient client = new WebClient())
                {
                    return client.DownloadData(url);
                }
            }

            void BuildSeal(Picture pict, String seal)
            {
                if (seal != null)
                {
                    byte[]? data = null;
                    if (seal.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                    {
                        data = GetImage(seal);
                    }
                    else if (seal.StartsWith("data", StringComparison.InvariantCultureIgnoreCase))
                    {
                        data = Convert.FromBase64String(seal.Substring(seal.IndexOf(',') + 1));
                    }

                    if (data != null)
                    {
                        MemoryStream stream = new MemoryStream(data);
                        pict.PictureStream = stream;
                        using (System.Drawing.Image img = System.Drawing.Image.FromStream(stream))
                        {
                            pict.Layout.Size = new GemBox.Document.Size(img.Width / (Settings.Default.SealImageDPI ?? img.HorizontalResolution), img.Height / (Settings.Default.SealImageDPI ?? img.VerticalResolution), LengthUnit.Inch);
                        }
                    }
                }
            }

            ComponentInfo.SetLicense(Settings.Default.GemboxKey);

            // Load Word document from file's path.
            var templatePath = System.IO.Path.Combine(FileLogger.Logger.LogPath, "Template").CheckStoredPath();
            var document = DocumentModel.Load(System.IO.Path.Combine(templatePath, Settings.Default.TemplateContractDocx));

            if (Settings.Default.LeftMargins.HasValue && document.Sections.Count > 0)
            {
                document.Sections[0].PageSetup.PageMargins.Left = document.Sections[0].PageSetup.PageMargins.Right = Settings.Default.LeftMargins.Value;
            }

            ReplaceToken(document);

            foreach (var p in document.GetChildElements(true, ElementType.Paragraph))
            {
                ((Paragraph)p).ParagraphFormat.LineSpacing = Settings.Default.LineSpacing;
                //ReplaceToken(p);
            }

            var elements = document.GetChildElements(true, ElementType.Picture).ToList();
            if (elements.Count > 0)
            {
                BuildSeal((Picture)elements[0], forms["BuyerSeal"]);
            }
            if (elements.Count > 1)
            {
                BuildSeal((Picture)elements[1], forms["SellerSeal"]);
            }

            //foreach(var li in document.CalculateListItems())
            //{
            //    li.Inlines[0].Content.Replace("(1)", "(一)");
            //}

            String pdfPath = Path.Combine(FileLogger.Logger.LogDailyPath, $"{Guid.NewGuid()}.pdf");
            document.Save(pdfPath);

            return PhysicalFile(pdfPath, "application/pdf");
        }

        [AllowAnonymous]
        public ActionResult BuildContract(SignContractViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;
            viewModel.KeyID = viewModel.KeyID.GetEfficientString();
            if(viewModel.KeyID != null) 
            {
                if(System.IO.File.Exists(viewModel.KeyID))
                {
                    viewModel = JsonConvert.DeserializeObject<SignContractViewModel>(System.IO.File.ReadAllText(viewModel.KeyID));
                }
            }

            return View("~/Views/Home/BuildContract.cshtml", viewModel);
        }

        [AllowAnonymous]
		public ActionResult GetContract(SignContractViewModel viewModel)
		{
			ViewBag.ViewModel = viewModel;
            if (viewModel.UseTemplate != false)
            {
                Dictionary<String, StringValues> data = new Dictionary<string, StringValues>();
                data.Add("SignDate", viewModel.SignDate);
                data.Add("BuyerIdNo", viewModel.BuyerIdNo);
                data.Add("BuyerAddress", viewModel.BuyerAddress);
                data.Add("BuyerName", viewModel.BuyerName);
                data.Add("PayWeekDate", viewModel.PayWeekDate);
                data.Add("EndDate", viewModel.EndDate);
                data.Add("CreditDate", viewModel.CreditDate);
                data.Add("Amount", viewModel.Amount);
                data.Add("No", viewModel.ContractNo);
                data.Add("BuyerSeal", viewModel.BuyerSeal);
                data.Add("SellerSeal", viewModel.SellerSeal);

                return ApplyContract(new FormCollection(data), new ErrorViewModel { });
            }
            else
            {
                String tmpPath = Path.Combine(FileLogger.Logger.LogDailyPath, $"{Guid.NewGuid()}.json");
                System.IO.File.WriteAllText(tmpPath, viewModel.JsonStringify());
                var contractUrl = $"{Settings.Default.WebAppDomain}{Url.Action("BuildContract", "Home", new { KeyID = tmpPath })}";
                String pdfFile = Path.Combine(FileLogger.Logger.LogDailyPath, $"{Guid.NewGuid()}.pdf");
                contractUrl.ConvertHtmlToPDF(pdfFile, 20);
                if (System.IO.File.Exists(pdfFile))
                {
                    return new PhysicalFileResult(pdfFile, "application/pdf");
                }

                return new EmptyResult { };
            }
		}

        [AllowAnonymous]
        public async Task<ActionResult> DownloadContractAsync()
        {
            try
            {
                if (Request.ContentLength > 0)
                {
                    using(MemoryStream stream = await Request.GetRequestStreamAsync()) 
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.Load(stream);
                        SignContractViewModel viewModel = doc.ConvertTo<SignContractViewModel>();
                        return GetContract(viewModel);
                    }
                }
               
            }
            catch (Exception ex) 
            {
                FileLogger.Logger.Error(ex);
            }

            return new EmptyResult { };
        }

        public ActionResult SearchCompany(String? term)
        {
            IQueryable<Organization> items = models.GetTable<Organization>();

            if (!String.IsNullOrEmpty(term))
            {
                items = items
                    .Where(f => f.ReceiptNo.StartsWith(term) || f.CompanyName.Contains(term));
            }
            else
            {
                items = items.Where(f => false);
            }

            ViewBag.DataItems = items;

            return Json(items.OrderBy(o => o.ReceiptNo).ToArray()
                .Select(o => new
                {
                    label = $"{o.ReceiptNo} {o.CompanyName}",
                    value = o.CompanyID.EncryptKey()
                }));
        }

        public ActionResult VueSearchCompany([FromBody] QueryViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;
            SearchCompany(viewModel.Term);
            IQueryable<Organization> items = (IQueryable<Organization>)ViewBag.DataItems;
            return View("~/Views/Organization/VueModule/OrganizationItems.cshtml", items);
        }

    }
}