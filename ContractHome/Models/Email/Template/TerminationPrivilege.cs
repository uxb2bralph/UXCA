namespace ContractHome.Models.Email.Template
{
    public class TerminationPrivilege : IEmailContent
    {
        public string Subject => @"[UX SIGN]合約建立權限終止通知信";

        EmailBody IEmailContent.GetBody => this.EmailBody;

        private EmailBody EmailBody;

        private IEmailBodyBuilder _emailBodyBuilder;

        public TerminationPrivilege(IEmailBodyBuilder emailBodyBuilder)
        {
            _emailBodyBuilder = emailBodyBuilder;
        }

        public void CreateBody(string companyName, string mailTo)
        {
            EmailBody = _emailBodyBuilder
                        .SetTemplateItem(this.GetType().Name)
                        .SetCompanyName(companyName)
                        .SetSendUserEmail(mailTo)
                        .Build();
        }

        public void CreateBody(EmailContentBodyDto emailContentBodyDto)
        {
            throw new NotImplementedException();
        }
    }
}
