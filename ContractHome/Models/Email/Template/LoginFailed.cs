using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using ContractHome.Properties;
using DocumentFormat.OpenXml.Spreadsheet;
using static ContractHome.Models.Email.Template.EmailBody;

namespace ContractHome.Models.Email.Template
{
    public class LoginFailed : IEmailContent
    {
        public string Subject => @"[安心簽]登入失敗通知信";

        EmailBody IEmailContent.GetBody => this.EmailBody;

        private EmailBody EmailBody;

        private IEmailBodyBuilder _emailBodyBuilder;

        public LoginFailed(IEmailBodyBuilder emailBodyBuilder)
        {
            _emailBodyBuilder = emailBodyBuilder;
        }

        public void CreateBody(string emailUserName, string mailTo)
        {
            EmailBody = _emailBodyBuilder
                .SetTemplateItem(this.GetType().Name)
                .SetSendUserName(emailUserName)
                .SetSendUserEmail(mailTo)
            .Build();

        }

        public void CreateBody(EmailContentBodyDto emailContentBodyDto)
        {
            throw new NotImplementedException();
        }
    }
}
