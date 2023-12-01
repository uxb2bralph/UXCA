namespace ContractHome.Models.Email.Template
{
    public class EmailBodyTemplateBuilder : IEmailBodyTemplateBuilder
    {
        private EmailBodyTemplate _emailTemplate;
        private string _contractNo;
        private string _title;
        private string _initiatorUserName;
        private string _userEmail;
        private string _userName;
        private string _templateItem;

        public EmailBodyTemplateBuilder(EmailBodyTemplate emailBodyTemplate) 
        {
            _emailTemplate = emailBodyTemplate;
        }
        public EmailBodyTemplate Build()
        {
            return _emailTemplate;
        }

        public IEmailBodyTemplateBuilder GetTemplateView()
        {
            return this;
        }

        public IEmailBodyTemplateBuilder SetContractLink(string contractLink)
        {
            _emailTemplate.SetContractLink(contractLink);
            return this;
        }

        public IEmailBodyTemplateBuilder SetContractNo(string contractNo)
        {
            _emailTemplate.SetContractNo(contractNo);
            return this;
        }

        public IEmailBodyTemplateBuilder SetRecipientUserEmail(string recipientUserEmail)
        {
            _emailTemplate.SetRecipientUserEmail(recipientUserEmail);
            return this;
        }

        public IEmailBodyTemplateBuilder SetRecipientUserName(string recipientUserName)
        {
            _emailTemplate.SetRecipientUserName(recipientUserName);
            return this;
        }

        public IEmailBodyTemplateBuilder SetTemplateItem(string templateItem)
        {
            _emailTemplate.SetTemplateItem(templateItem);
            return this;
        }

        public IEmailBodyTemplateBuilder SetTitle(string title)
        {
            _emailTemplate.SetTitle(title);
            return this;
        }

        public IEmailBodyTemplateBuilder SetUserEmail(string userEmail)
        {
            _emailTemplate.SetUserEmail(userEmail);
            return this;
        }

        public IEmailBodyTemplateBuilder SetUserName(string userName)
        {
            _emailTemplate.SetUserName(userName);
            return this;
        }

        public IEmailBodyTemplateBuilder SetVerifyLink(string verifyLink)
        {
            _emailTemplate.SetVerifyLink(verifyLink);
            return this;
        }
    }
}
