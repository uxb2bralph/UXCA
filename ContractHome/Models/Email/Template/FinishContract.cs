using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using ContractHome.Properties;
using DocumentFormat.OpenXml.Spreadsheet;
using static ContractHome.Models.Email.Template.EmailBody;

namespace ContractHome.Models.Email.Template
{
  public class FinishContract : IEmailContent
  {
    public string Subject => @"[UX SIGN]完成通知信";

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
        JwtTokenGenerator.JwtPayloadData jwtPayloadData = new JwtTokenGenerator.JwtPayloadData()
        {
            UID = emailContentBodyDto.UserProfile.UID.EncryptKey(),
            Email = emailContentBodyDto.UserProfile.EMail,
            ContractID = emailContentBodyDto.Contract.ContractID.EncryptKey(),
            Func = this.GetType().Name
        };

        var jwtToken = JwtTokenGenerator.GenerateJwtToken(jwtPayloadData, 20160);
        var downloadContractLink = $"{Settings.Default.WebAppDomain}/api/ContractDownload/DownloadContract?token={JwtTokenGenerator.Base64UrlEncode((jwtToken.EncryptData()))}";
        var downloadFootprintsLink = $"{Settings.Default.WebAppDomain}/api/ContractDownload/DownloadFootprints?token={JwtTokenGenerator.Base64UrlEncode((jwtToken.EncryptData()))}";

        this.EmailBody = _emailBodyBuilder
        .SetTemplateItem(this.GetType().Name)
        .SetContractNo(emailContentBodyDto.Contract.ContractNo)
        .SetTitle(emailContentBodyDto.Contract.Title)
        .SetRecipientUserName(emailContentBodyDto.UserProfile.PID)
        .SetRecipientUserEmail(emailContentBodyDto.UserProfile.EMail)
        .SetDownloadContractLink(downloadContractLink)
        .SetDownloadFootprintsLink(downloadFootprintsLink)
        .Build();
    }
  }
}
