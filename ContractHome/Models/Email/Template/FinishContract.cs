using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using ContractHome.Properties;
using DocumentFormat.OpenXml.Spreadsheet;
using static ContractHome.Models.Email.Template.EmailBody;

namespace ContractHome.Models.Email.Template
{
    public class FinishContract : IEmailContent
    {
        public string Subject => @"[安心簽]完成通知信";

        EmailBody IEmailContent.GetBody => this.EmailBody;

        private EmailBody EmailBody;

        private IEmailBodyBuilder _emailBodyBuilder;

        public FinishContract(IEmailBodyBuilder emailBodyBuilder)
        {
            _emailBodyBuilder = emailBodyBuilder;
        }

        public void CreateBody(string emailUserName, string mailTo)
        {
            throw new NotImplementedException();
        }

        public void CreateBody(EmailContentBodyDto emailContentBodyDto)
        {
            this.EmailBody = _emailBodyBuilder
                    .SetTemplateItem(this.GetType().Name)
                    .SetContractNo(emailContentBodyDto.Contract.ContractNo)
                    .SetTitle(emailContentBodyDto.Contract.Title)
                    .SetRecipientUserName(emailContentBodyDto.UserProfile.PID)
                    .SetRecipientUserEmail(emailContentBodyDto.UserProfile.EMail)
                    .Build();
        }
    }
}
