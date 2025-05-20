using CommonLib.Core.Properties;
using CommonLib.Core.Utility;
using CommonLib.DataAccess;
using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using ContractHome.Models.Dto;
using ContractHome.Models.Email;
using ContractHome.Models.Email.Template;
using ContractHome.Models.Helper;
using ContractHome.Services.ContractService;
using ContractHome.Services.HttpChunk;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Diagnostics.Contracts;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Web;
using Wangkanai.Extensions;
using static ContractHome.Helper.JwtTokenGenerator;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ContractHome.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TryController : ControllerBase
    {

        private readonly IMailService _mailService;
        private readonly IViewRenderService _viewRenderService;
        private readonly IRazorViewToStringRenderer _razorViewToStringRenderer;
        private ContractServices? _contractServices;
        private IWebHostEnvironment _hostingEnvironment;
        private readonly IHttpChunkService _httpChunkService;
        private readonly KNFileUploadSetting _KNFileUploadSetting;
        private readonly ICustomContractService _customContractService;
        private readonly ChunkFileUploader _chunkFileUploader;
        private readonly EmailFactory _emailContentFactories;
        public TryController(
            IMailService mailService,
            IViewRenderService viewRenderService,
            ContractServices contractServices,
            IWebHostEnvironment environment
            , IHttpChunkService httpChunkService
            , IOptions<KNFileUploadSetting> kNFileUploadSetting
            , ICustomContractService customContractService
            , ChunkFileUploader chunkFileUploader
            , EmailFactory emailContentFactories

            )
        {
            _contractServices = contractServices;
            _mailService = mailService;
            _viewRenderService = viewRenderService;
            _hostingEnvironment = environment;
            _httpChunkService = httpChunkService;
            _KNFileUploadSetting = kNFileUploadSetting.Value;
            _customContractService = customContractService;
            _chunkFileUploader = chunkFileUploader;
            _emailContentFactories = emailContentFactories;
        }

        public class VO
        {
            public string StringItem { get; set; }
            public int IntItem { get; set; }
        }

        [HttpGet]
        [Route("EasyPass")]
        public async Task<IActionResult> EasyPass([FromQuery] string pid)
        {
            //wait to add... if debug
            DCDataContext models = new DCDataContext();
            var user = models.GetTable<UserProfile>().Where(x => x.PID == pid).FirstOrDefault();
            await HttpContext.SignOnAsync(user);
            return Redirect("https://localhost:5153/ContractConsole/ListToStampIndex");
        }

        [HttpGet]
        [Route("DecryptKeyValue")]
        public IActionResult GetDecryptKeyValue([FromQuery] string keyID)
        {
            //viewModel.ContractID = viewModel.DecryptKeyValue();
            return Ok(keyID.DecryptKeyValue());
        }

        [HttpGet]
        [Route("SaveFile")]
        public async Task<IActionResult> SaveFile(IFormFile file)
        {
            await NewMethod(file);
            return Ok();
        }

        private async Task<string> NewMethod(IFormFile file)
        {
            string uploads = Path.Combine(_hostingEnvironment.WebRootPath, "logs");
            string filePath = Path.Combine(uploads, file.FileName);
            using (Stream fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            return filePath;
        }

        [HttpGet]
        [Route("void")]
        public IActionResult GetVoidDocument(IFormFile img, IFormFile pdf)
        {
            if ((img.Length > 0))
            {
                var filePath = NewMethod(pdf).Result;
                var pdfFile = new FileInfo(filePath);
                using (var ms = new MemoryStream())
                {
                    img.CopyTo(ms);
                    var fileBytes = ms.ToArray();
                    PdfDocument pdfDocument = PdfDocument.FromFile(filePath);
                    IronPdfExtensions.ApplyStamp(pdfDocument, fileBytes, 1, 1, 1.01, 0);
                    string extName = $"{Guid.NewGuid()}{Path.GetExtension(pdfFile.FullName)}";
                    string uploads = Path.Combine(_hostingEnvironment.WebRootPath, "logs", extName);
                    pdfDocument.SaveAs(uploads);
                    return Ok();
                    //string s = Convert.ToBase64String(fileBytes);
                    // act on the Base64 data
                }
            }
            return BadRequest();
        }

        [HttpPost]
        [Route("GetEncryptUserID")]
        public IActionResult GetEncryptUserID([FromBody] VO targetString)
        {
            try
            {
                var encString = targetString.StringItem.EncryptData();
                var urlEncodeEncString = HttpUtility.UrlEncode(encString);
                return Ok(urlEncodeEncString);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return BadRequest();
            }


        }

        [HttpPost]
        [Route("GetEncryptContractID")]
        public IActionResult GetEncryptContractID([FromBody] VO targetString)
        {
            try
            {
                //Contract contract = new Contract();
                //contract.ContractID = targetString.IntItem;
                //var aaa = targetString.IntItem;
                var encString = targetString.IntItem.EncryptKey();
                //var encContractString = contract.ContractID.EncryptKey();
                //var urlEncodeEncString = HttpUtility.UrlEncode(encString);
                return Ok(encString);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return BadRequest();
            }


        }

        [HttpGet]
        [Route("TokenValidate")]
        public IActionResult GetTokenValidate([FromQuery] string tokenString)
        {
            string result = string.Empty;
            var a = JwtTokenValidator.Base64UrlDecodeToString(tokenString);
            var b = a.DecryptData();


            if (string.IsNullOrEmpty(b))
            {
                result = "驗證資料為空值。";
            }

            if (!JwtTokenValidator.ValidateJwtToken(b, JwtTokenGenerator.secretKey))
            {
                result += "Token已失效，請重新申請。";
            }
            var jwtTokenObj = JwtTokenValidator.DecodeJwtToken(b);
            if (jwtTokenObj == null)
            {
                result += "Token已失效，請重新申請。";
            }

            return Ok(jwtTokenObj);
        }

        /// <summary>
        /// 分片下載
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("ChunkDownload")]
        public async Task<IActionResult> ChunkDownload()
        {
            //HttpChunkResult chunkResult = new();

            //try
            //{
            //    chunkResult = await _httpChunkService.DownloadAsync(Request);
            //}
            //catch (Exception ex)
            //{
            //    FileLogger.Logger.Error(ex.ToString());
            //    chunkResult.Code = (int)HttpChunkResultCodeEnum.SYSTEM_ERROR;
            //    chunkResult.Message = ex.ToString();
            //}

            //if (chunkResult.Code != (int)HttpChunkResultCodeEnum.COMPLETE)
            //{
            //    return Ok(new ContractResultModel()
            //    {
            //        msgRes = new MsgRes()
            //        {
            //            type = ContractResultType.E.ToString(),
            //            code = ICustomContractService.ResultCodeHeader + ContractResultCode.ContractUpdate,
            //            desc = chunkResult.Message
            //        }
            //    });
            //}

            return Ok(await _customContractService.DownloadAsync(Request));
        }

        /// <summary>
        /// 分片上傳
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("ChuckUpload")]
        public async Task<IActionResult> ChuckUploadAsync()
        {
            //HttpChunkResult chunkResult = new();
            //try
            //{
            //    chunkResult = await _httpChunkService.UploadAsync(
            //                   Path.Combine(Directory.GetCurrentDirectory(), "6.9M.pdf"),
            //                    $"{_KNFileUploadSetting.ContractQueueid}_123_{_KNFileUploadSetting.FileCurrentDateTime}",
            //                   Properties.Settings.Default.HttpChunkUploadUrl);
            //}
            //catch (Exception ex)
            //{
            //    FileLogger.Logger.Error(ex.ToString());
            //    chunkResult.Code = (int)HttpChunkResultCodeEnum.SYSTEM_ERROR;
            //    chunkResult.Message = ex.ToString();
            //}
            try
            {
                await _chunkFileUploader.UploadAsync(filePath: Path.Combine(Directory.GetCurrentDirectory(), "6.9M.pdf"));
            }
            catch (Exception ex) {
                FileLogger.Logger.Error(ex.ToString());
                return BadRequest(ex.ToString());
            }

            return Ok();
        }

        [HttpPost]
        [Route("ContractInfo")]
        public async Task<IActionResult> ContractInfo([FromBody] ContractModel contractModel)
        {
            try
            {
                if (!_customContractService.IsValid(contractModel, ModelState, out ContractResultModel result))
                {
                    return Ok(result);
                }
                var createResult = _customContractService.CreateContract(contractModel);
                return Ok(createResult);
            }
            catch (Exception ex)
            {
                FileLogger.Logger.Error(ex.ToString());
                return Ok(new ContractResultModel()
                {
                    msgRes = new MsgRes()
                    {
                        type = ContractResultType.E.ToString(),
                        code = ContractResultCode.ContractCreate.GetFullCode(),
                        desc = ex.ToString()
                    }
                });
            }
            
        }

        [HttpGet]
        [Route("CreatePDF")]
        public async Task<IActionResult> CreatePDF()
        {
            DCDataContext models = new DCDataContext();
            var contract = models.GetTable<Models.DataEntity.Contract>().Where(x => x.ContractID == 5200).FirstOrDefault();
            if (contract == null)
            {
                return NotFound("Contract not found");
            }

            //var tasks = new List<Task<string>>
            //{
            //     _customContractService.CreateSignaturePDF(contract),
            //     _customContractService.CreateFootprintsPDF(contract)
            //};
            //var results = await Task.WhenAll(tasks);

            await _customContractService.UploadSignatureAndFootprintsPdfFile(contract);

            return Ok();
        }

        [HttpGet]
        [Route("SendEmail")]
        public async Task<IActionResult> SendEmail()
        {
            
            //await _contractServices.NotifyTerminationContract();

            return Ok();
        }
    }
}
