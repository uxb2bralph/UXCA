using static ContractHome.Models.Email.Template.EmailBody;

namespace ContractHome.Models.Email.Template
{
    public interface IEmailBodyBuilder
    {
        IEmailBodyBuilder SetTemplateItem(EmailTemplate emailTemplate);
        IEmailBodyBuilder GetTemplateView();
        IEmailBodyBuilder SetContractNo(string contractNo);
        IEmailBodyBuilder SetTitle(string title);
        IEmailBodyBuilder SetUserName(string userName);
        IEmailBodyBuilder SetUserEmail(string userEmail);
        IEmailBodyBuilder SetRecipientUserName(string recipientUserName);
        IEmailBodyBuilder SetRecipientUserEmail(string recipientUserEmail);
        IEmailBodyBuilder SetContractLink(string contractLink);
        IEmailBodyBuilder SetVerifyLink(string verifyLink);
        EmailBody Build();
    }
}
