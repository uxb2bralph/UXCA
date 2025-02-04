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
    public string Subject => @"[UX SIGN]密碼補發通知信";

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
      var jwtToken = JwtTokenGenerator.GenerateJwtToken(uid: emailContentBodyDto.UserProfile.UID , func: this.GetType().Name);
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
