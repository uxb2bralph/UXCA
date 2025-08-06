using static ContractHome.Models.Email.Template.EmailBody;

namespace ContractHome.Models.Email.Template
{
    public interface IEmailBodyBuilder
    {
        IEmailBodyBuilder SetTemplateItem(string viewName);
        IEmailBodyBuilder GetTemplateView();
        IEmailBodyBuilder SetContractNo(string contractNo);
        IEmailBodyBuilder SetTitle(string title);
        IEmailBodyBuilder SetSendUserName(string userName);
        IEmailBodyBuilder SetSendUserEmail(string userEmail);
        IEmailBodyBuilder SetRecipientUserName(string recipientUserName);
        IEmailBodyBuilder SetRecipientUserEmail(string recipientUserEmail);
        IEmailBodyBuilder SetContractLink(string contractLink);
        IEmailBodyBuilder SetVerifyLink(string verifyLink);

        IEmailBodyBuilder SetDownloadContractLink(string downloadLink);

        IEmailBodyBuilder SetDownloadFootprintsLink(string downloadLink);

        EmailBody Build();
    }
}
