using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using ContractHome.Properties;
using DocumentFormat.OpenXml.Spreadsheet;
using static ContractHome.Models.Email.Template.EmailBody;

namespace ContractHome.Models.Email.Template
{
  public class TaskNotifySign : IEmailContent
  {
    public string Subject => @"[UX SIGN]簽署通知信";

    EmailBody IEmailContent.GetBody => this.EmailBody;

    private EmailBody EmailBody;

    private IEmailBodyBuilder _emailBodyBuilder;

    public TaskNotifySign(IEmailBodyBuilder emailBodyBuilder)
    {
      _emailBodyBuilder = emailBodyBuilder;
    }

    public void CreateBody(string emailUserName, string mailTo)
    {
      throw new NotImplementedException();
    }

    public void CreateBody(EmailContentBodyDto emailContentBodyDto)
    {
            var jwtToken = JwtTokenGenerator.GenerateJwtToken(uid: emailContentBodyDto.UserProfile.UID,
                contractID: emailContentBodyDto.Contract.ContractID,
                func: this.GetType().Name);
            var clickLink = $"{Settings.Default.WebAppDomain}/Task/Trust?token={JwtTokenGenerator.Base64UrlEncode((jwtToken.EncryptData()))}";

      this.EmailBody = _emailBodyBuilder
              .SetTemplateItem(this.GetType().Name)
              .SetContractNo(emailContentBodyDto.Contract.ContractNo)
              .SetTitle(emailContentBodyDto.Contract.Title)
              .SetSendUserName(emailContentBodyDto.InitiatorOrg.CompanyName)
              //.SetRecipientUserName(emailContentBodyDto.UserProfile.PID)
              .SetRecipientUserEmail(emailContentBodyDto.UserProfile.EMail)
              .SetContractLink(clickLink)
              .Build();
    }
  }
}
