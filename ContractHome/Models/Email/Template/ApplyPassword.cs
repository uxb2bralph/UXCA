using CommonLib.Core.Utility;
using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using ContractHome.Properties;
using DocumentFormat.OpenXml.Spreadsheet;
using static ContractHome.Helper.JwtTokenGenerator;
using static ContractHome.Models.Email.Template.EmailBody;

namespace ContractHome.Models.Email.Template
{
    public class ApplyPassword : IEmailContent
    {
        public string Subject => @"[安心簽]密碼補發通知信";

        EmailBody IEmailContent.GetBody => this.EmailBody;

        private EmailBody EmailBody;

        private IEmailBodyBuilder _emailBodyBuilder;

        public ApplyPassword(IEmailBodyBuilder emailBodyBuilder)
        {
            _emailBodyBuilder = emailBodyBuilder;
        }

        public void CreateBody(string emailUserName, string mailTo)
        {
            throw new NotImplementedException();
        }

        public void CreateBody(EmailContentBodyDto emailContentBodyDto)
        {
            JwtPayloadData jwtPayloadData = new JwtPayloadData()
            {
                ContractID = string.Empty,
                Email = emailContentBodyDto.UserProfile.EMail,
                UID = emailContentBodyDto.UserProfile.UID.EncryptKey()
            };
            var jwtToken = JwtTokenGenerator.GenerateJwtToken(jwtPayloadData, 4320);
            FileLogger.Logger.Info($"{this.GetType().Name}-Token={jwtToken}");
            FileLogger.Logger.Info($"{this.GetType().Name}-Base64UrlEncodeToken={Base64UrlEncode(jwtToken.EncryptData())}");
            var clickLink = $"{Settings.Default.WebAppDomain}/Account/Trust?token={Base64UrlEncode(jwtToken.EncryptData())}";

            this.EmailBody = _emailBodyBuilder
                    .SetTemplateItem(this.GetType().Name)
                    .SetSendUserName(emailContentBodyDto.UserProfile.PID)
                    .SetSendUserEmail(emailContentBodyDto.UserProfile.EMail)
                    .SetVerifyLink(clickLink)
                    .Build();
        }
    }
}
