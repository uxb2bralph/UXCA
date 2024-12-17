using CommonLib.Core.Properties;
using CommonLib.Core.Utility;
using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using ContractHome.Models.Dto;
using ContractHome.Models.Email;
using ContractHome.Models.Email.Template;
using ContractHome.Models.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Diagnostics.Contracts;
using System.DirectoryServices.Protocols;
using System.Web;
using Wangkanai.Extensions;
using static ContractHome.Helper.JwtTokenGenerator;

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

        public TryController(
            IMailService mailService,
            IViewRenderService viewRenderService,
            ContractServices contractServices,
            IWebHostEnvironment environment
            )
        {
            _contractServices = contractServices;
            _mailService = mailService;
            _viewRenderService = viewRenderService;
            _hostingEnvironment = environment;
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
                    String extName = $"{Guid.NewGuid()}{Path.GetExtension(pdfFile.FullName)}";
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

    }
}
