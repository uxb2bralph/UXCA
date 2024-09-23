using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using ContractHome.Properties;
using DocumentFormat.OpenXml.Spreadsheet;
using static ContractHome.Models.Email.Template.EmailBody;

namespace ContractHome.Models.Email.Template
{
  public class LoginSuccessed : IEmailContent
  {
    public string Subject => @"[UX SIGN]登入成功通知信";

    EmailBody IEmailContent.GetBody => this.EmailBody;

    public string GetSubject => throw new NotImplementedException();

    private EmailBody EmailBody;

    private IEmailBodyBuilder _emailBodyBuilder;

    public LoginSuccessed(IEmailBodyBuilder emailBodyBuilder)
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
