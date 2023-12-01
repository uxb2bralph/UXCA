namespace ContractHome.Models.Email.Template
{
    public interface IEmailBodyTemplateBuilder
    {
        IEmailBodyTemplateBuilder SetTemplateItem(string templateItem);
        IEmailBodyTemplateBuilder GetTemplateView();
        IEmailBodyTemplateBuilder SetContractNo(string contractNo);
        IEmailBodyTemplateBuilder SetTitle(string title);
        IEmailBodyTemplateBuilder SetUserName(string userName);
        IEmailBodyTemplateBuilder SetUserEmail(string userEmail);
        IEmailBodyTemplateBuilder SetRecipientUserName(string recipientUserName);
        IEmailBodyTemplateBuilder SetRecipientUserEmail(string recipientUserEmail);
        IEmailBodyTemplateBuilder SetContractLink(string contractLink);
        IEmailBodyTemplateBuilder SetVerifyLink(string verifyLink);
        EmailBodyTemplate Build();
    }
}
