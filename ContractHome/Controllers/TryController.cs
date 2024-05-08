using CommonLib.Core.Properties;
using CommonLib.Core.Utility;
using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using ContractHome.Models.Email;
using ContractHome.Models.Email.Template;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.DirectoryServices.Protocols;
using System.Web;

namespace ContractHome.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TryController : ControllerBase
    {

        private readonly IMailService _mailService;
        //private readonly IEnumerable<IEmailTemplate> _emailTemplate;
        private readonly IViewRenderService _viewRenderService;
        //private readonly EmailDataFactory _emailDataFactory;
        //private readonly EmailBodyTemplate _emailBodyTemplate;
        private readonly IRazorViewToStringRenderer _razorViewToStringRenderer;

        public TryController(
            IMailService mailService,
            //IEnumerable<IEmailTemplate> emailTemplate,
            IViewRenderService viewRenderService
            //EmailDataFactory emailDataFactory,
            //IRazorViewToStringRenderer razorViewToStringRenderer,
            //EmailBodyTemplate emailBodyTemplate
            )
        {

            _mailService = mailService;
            //_emailTemplate = emailTemplate;
            //_emailDataFactory = emailDataFactory;
            _viewRenderService = viewRenderService;
            //_razorViewToStringRenderer = razorViewToStringRenderer;
            //_emailBodyTemplate = emailBodyTemplate;
        }

        public class VO
        {
            public string StringItem { get; set; }
            public int IntItem { get; set; }
        }

        //[HttpPost]
        //[Route("TestRazorViewToStringRenderer")]
        //public async Task<IActionResult> TestRazorViewToStringRenderer(VO sendMailData)
        //{
        //    TemplateShopExpired welcome = new TemplateShopExpired();
        //    welcome.ShopHost = "a";
        //    welcome.ShopHostAdmin = "a";
        //    welcome.ShopSaveDays = "a";
        //    welcome.ShopCloseDate = "a";
        //    welcome.CdnHost = "a";
        //    var ttt = await _razorViewToStringRenderer.RenderViewToStringAsync(
        //        viewName: sendMailData.StringItem,
        //        model: welcome);
        //    return Ok(ttt.Length);
        //}

        //[HttpPost]
        //[Route("TestViewRenderService")]
        //public async Task<IActionResult> TestViewRenderService(VO sendMailData)
        //{
        //    TemplateShopExpired welcome = new TemplateShopExpired();
        //    welcome.ShopHost = "a";
        //    welcome.ShopHostAdmin = "a";
        //    welcome.ShopSaveDays = "a";
        //    welcome.ShopCloseDate = "a";
        //    welcome.CdnHost = "a";
        //    string ttt = await _viewRenderService.RenderToStringAsync(
        //        viewName: sendMailData.StringItem,
        //        model: welcome);
        //    return Ok(ttt.Length);
        //}
        //[HttpPost]
        //[Route("SendTemplateEMail")]
        //public async Task<bool> SendTemplateEMail([FromBody] VO sendMailData)
        //{
        //    try
        //    {
        //        EmailBodyTemplate emailDataTemplate =
        //            new EmailBodyTemplateBuilder(_emailBodyTemplate)
        //            .SetTemplateItem(sendMailData.StringItem)
        //            .SetContractNo("ContractNo.12345")
        //            .SetTitle("this is title.")
        //            .SetUserName("IAmIris.")
        //            .SetUserEmail("iris@uxb2b.com.")
        //            .SetRecipientUserName("IAmPartyB.")
        //            .SetRecipientUserEmail("partyB@uxb2b.com")
        //            .SetContractLink(@"https://localhost:5153/account/login")
        //            .SetVerifyLink(@"https://localhost:5153/account/login")
        //            .Build();

        //        var mailData = _emailDataFactory.GetMailDataToCustomer(
        //            email: "iris@uxb2b.com",
        //            subject: $@"{sendMailData.StringItem}",
        //            body: await emailDataTemplate.GetViewRenderString());

        //        return await _mailService.SendMailAsync(mailData);

        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.ToString());
        //        return false;
        //    }


        //}


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
    }
}
