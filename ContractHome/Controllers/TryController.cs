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

namespace ContractHome.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TryController : ControllerBase
    {

        private readonly IMailService _mailService;
        //private readonly IEnumerable<IEmailTemplate> _emailTemplate;
        private readonly IViewRenderService _viewRenderService;
        private readonly EmailDataFactory _emailDataFactory;
        private readonly EmailBodyTemplate _emailBodyTemplate;
        private readonly IRazorViewToStringRenderer _razorViewToStringRenderer;

        public TryController(
            IMailService mailService,
            //IEnumerable<IEmailTemplate> emailTemplate,
            IViewRenderService viewRenderService,
            EmailDataFactory emailDataFactory,
            //IRazorViewToStringRenderer razorViewToStringRenderer,
            EmailBodyTemplate emailBodyTemplate)
        {

            _mailService = mailService;
            //_emailTemplate = emailTemplate;
            _emailDataFactory = emailDataFactory;
            _viewRenderService = viewRenderService;
            //_razorViewToStringRenderer = razorViewToStringRenderer;
            _emailBodyTemplate = emailBodyTemplate;
        }

        public class SendMailData
        {
            public string item { get; set; }
        }

        [HttpPost]
        [Route("TestRazorViewToStringRenderer")]
        public async Task<IActionResult> TestRazorViewToStringRenderer(SendMailData sendMailData)
        {
            TemplateShopExpired welcome = new TemplateShopExpired();
            welcome.ShopHost = "a";
            welcome.ShopHostAdmin = "a";
            welcome.ShopSaveDays = "a";
            welcome.ShopCloseDate = "a";
            welcome.CdnHost = "a";
            var ttt  = await _razorViewToStringRenderer.RenderViewToStringAsync(
                viewName: sendMailData.item,
                model: welcome);
            return Ok(ttt.Length);
        }

        [HttpPost]
        [Route("TestViewRenderService")]
        public async Task<IActionResult> TestViewRenderService(SendMailData sendMailData)
        {
            TemplateShopExpired welcome = new TemplateShopExpired();
            welcome.ShopHost = "a";
            welcome.ShopHostAdmin = "a";
            welcome.ShopSaveDays = "a";
            welcome.ShopCloseDate = "a";
            welcome.CdnHost = "a";
            string ttt= await _viewRenderService.RenderToStringAsync(
                viewName: sendMailData.item,
                model: welcome);
            return Ok(ttt.Length);
        }
        [HttpPost]
        [Route("SendTemplateEMail")]
        public async Task<bool> SendTemplateEMail([FromBody] SendMailData sendMailData)
        {
            try
            {
                EmailBodyTemplate emailDataTemplate =
                    new EmailBodyTemplateBuilder(_emailBodyTemplate)
                    .SetTemplateItem(sendMailData.item)
                    .SetContractNo("ContractNo.12345")
                    .SetTitle("this is title.")
                    .SetUserName("IAmIris.")
                    .SetUserEmail("iris@uxb2b.com.")
                    .SetRecipientUserName("IAmPartyB.")
                    .SetRecipientUserEmail("partyB@uxb2b.com")
                    .SetContractLink(@"https://localhost:5153/account/login")
                    .SetVerifyLink(@"https://localhost:5153/account/login")
                    .Build();

                var mailData = _emailDataFactory.GetMailDataToCustomer(
                    email: "iris@uxb2b.com",
                    subject: $@"{sendMailData.item}",
                    body: await emailDataTemplate.GetViewRenderString());

                return await _mailService.SendMailAsync(mailData);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }


        }

    }
}
