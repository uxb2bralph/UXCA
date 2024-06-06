using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using ContractHome.Properties;
using DocumentFormat.OpenXml.Spreadsheet;
using static ContractHome.Models.Email.Template.EmailBody;

namespace ContractHome.Models.Email.Template
{
    public class NotifySign : IEmailContent
    {
        public string Subject => @"[安心簽]簽署通知信";

        EmailBody IEmailContent.GetBody => this.EmailBody;

        private EmailBody EmailBody;

        private IEmailBodyBuilder _emailBodyBuilder;

        public NotifySign(IEmailBodyBuilder emailBodyBuilder)
        {
            _emailBodyBuilder = emailBodyBuilder;
        }

        public void CreateBody(string emailUserName, string mailTo)
        {
            throw new NotImplementedException();
        }

        public void CreateBody(EmailContentBodyDto emailContentBodyDto)
        {
            JwtTokenGenerator.JwtPayloadData jwtPayloadData = new JwtTokenGenerator.JwtPayloadData()
            {
                UID = emailContentBodyDto.UserProfile.UID.EncryptKey(),
                Email = emailContentBodyDto.UserProfile.EMail,
                ContractID = emailContentBodyDto.Contract.ContractID.EncryptKey(),
                Func = this.GetType().Name
            };

            var jwtToken = JwtTokenGenerator.GenerateJwtToken(jwtPayloadData, 4320);
            var clickLink = $"{Settings.Default.WebAppDomain}/ContractConsole/Trust?token={JwtTokenGenerator.Base64UrlEncode((jwtToken.EncryptData()))}";

            this.EmailBody = _emailBodyBuilder
                    .SetTemplateItem(this.GetType().Name)
                    .SetContractNo(emailContentBodyDto.Contract.ContractNo)
                    .SetTitle(emailContentBodyDto.Contract.Title)
                    .SetSendUserName(emailContentBodyDto.InitiatorOrg.CompanyName)
                    .SetRecipientUserName($"{emailContentBodyDto.UserProfile.CompanyName} {emailContentBodyDto.UserProfile.UserName}")
                    .SetRecipientUserEmail(emailContentBodyDto.UserProfile.EMail)
                    .SetContractLink(clickLink)
                    .Build();
        }
    }
}
