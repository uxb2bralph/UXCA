using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using ContractHome.Properties;
using DocumentFormat.OpenXml.Spreadsheet;
using static ContractHome.Models.Email.Template.EmailBody;

namespace ContractHome.Models.Email.Template
{
  public class PasswordUpdated : IEmailContent
  {
    EmailBody IEmailContent.GetBody => this.EmailBody;

    private EmailBody EmailBody;

    private IEmailBodyBuilder _emailBodyBuilder;

    public PasswordUpdated(IEmailBodyBuilder emailBodyBuilder)
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

    public string Subject => @"[UX SIGN]密碼更新完成通知信";
  }
}
