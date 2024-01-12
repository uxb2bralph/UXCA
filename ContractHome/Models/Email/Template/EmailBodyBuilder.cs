using ContractHome.Models.Helper;
using static ContractHome.Models.Email.Template.EmailBody;

namespace ContractHome.Models.Email.Template
{
    public class EmailBodyBuilder : IEmailBodyBuilder
    {
        private EmailBody _emailTemplate;
        private string _contractNo;
        private string _title;
        private string _initiatorUserName;
        private string _userEmail;
        private string _userName;
        private string _templateItem;

        public EmailBodyBuilder(EmailBody emailBodyTemplate) 
        {
            _emailTemplate = emailBodyTemplate;
        }
        public EmailBody Build()
        {
            return _emailTemplate;
        }

        public IEmailBodyBuilder GetTemplateView()
        {
            return this;
        }

        public IEmailBodyBuilder SetContractLink(string contractLink)
        {
            _emailTemplate.SetContractLink(contractLink);
            return this;
        }

        public IEmailBodyBuilder SetContractNo(string contractNo)
        {
            _emailTemplate.SetContractNo(contractNo);
            return this;
        }

        public IEmailBodyBuilder SetRecipientUserEmail(string recipientUserEmail)
        {
            _emailTemplate.SetRecipientUserEmail(recipientUserEmail);
            return this;
        }

        public IEmailBodyBuilder SetRecipientUserName(string recipientUserName)
        {
            _emailTemplate.SetRecipientUserName(recipientUserName);
            return this;
        }

        public IEmailBodyBuilder SetTemplateItem(EmailTemplate emailTemplate)
        {
            _emailTemplate.SetTemplateItem(emailTemplate);
            return this;
        }

        public IEmailBodyBuilder SetTitle(string title)
        {
            _emailTemplate.SetTitle(title);
            return this;
        }

        public IEmailBodyBuilder SetUserEmail(string userEmail)
        {
            _emailTemplate.SetUserEmail(userEmail);
            return this;
        }

        public IEmailBodyBuilder SetUserName(string userName)
        {
            _emailTemplate.SetUserName(userName);
            return this;
        }

        public IEmailBodyBuilder SetVerifyLink(string verifyLink)
        {
            _emailTemplate.SetVerifyLink(verifyLink);
            return this;
        }
    }
}
