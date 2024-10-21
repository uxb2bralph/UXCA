using CommonLib.Core.Properties;
using CommonLib.Core.Utility;
using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using ContractHome.Models.Dto;
using ContractHome.Models.Email;
using ContractHome.Models.Email.Template;
using ContractHome.Models.Helper;
using ContractHome.Models.ViewModel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Org.BouncyCastle.Ocsp;
using System.DirectoryServices.Protocols;
using System.Web;
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
        public TryController(
            IMailService mailService,
            IViewRenderService viewRenderService,
            ContractServices contractServices
            )
        {
            _contractServices = contractServices;
            _mailService = mailService;
            _viewRenderService = viewRenderService;
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
        [Route("EasyPass")]
        public async Task<IActionResult> EasyPass([FromQuery] string pid)
        {
            DCDataContext models = new DCDataContext();
            var user =  models.GetTable<UserProfile>().Where(x => x.PID == pid).FirstOrDefault();
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

        [HttpPost]
        [Route("UserProfile")]
        public void PostUserProfile([FromBody] UserProfileViewModel viewModel)
        {
            DCDataContext models = new DCDataContext();
            UserProfile item = UserProfile.PrepareNewItem(models);

            item.PID = Guid.NewGuid().ToString();
            item.EMail = viewModel.EMail;
            //item.UserName = viewModel.UserName.GetEfficientString();
            item.Region = viewModel.Region;

            models.SubmitChanges();

            models.GetTable<OrganizationUser>().InsertOnSubmit(
                new OrganizationUser() { UID = item.UID, CompanyID = viewModel.GetCompanyID() ?? 0 });


            //if (viewModel.RoleID.HasValue)
            //{
            //    models.ExecuteCommand(@"DELETE FROM UserRole WHERE (UID = {0})", item.UID);
            models.ExecuteCommand(@"INSERT INTO UserRole (UID, RoleID) VALUES ({0},{1})", item.UID, 3);
            //}
            models.SubmitChanges();
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
