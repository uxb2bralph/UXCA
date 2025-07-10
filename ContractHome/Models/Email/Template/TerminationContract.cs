namespace ContractHome.Models.Email.Template
{
    public class TerminationContract : IEmailContent
    {
        public string Subject => @"[UX SIGN]合約終止通知信";

        EmailBody IEmailContent.GetBody => this.EmailBody;

        private EmailBody EmailBody;

        private IEmailBodyBuilder _emailBodyBuilder;

        public TerminationContract(IEmailBodyBuilder emailBodyBuilder)
        {
            _emailBodyBuilder = emailBodyBuilder;
        }

        public void CreateBody(string emailUserName, string mailTo)
        {
            throw new NotImplementedException();
        }

        public void CreateBody(EmailContentBodyDto emailContentBodyDto)
        {
            this.EmailBody = _emailBodyBuilder
                    .SetTemplateItem(this.GetType().Name)
                    .SetContractNo(emailContentBodyDto.Contract.ContractNo)
                    .SetTitle(emailContentBodyDto.Contract.Title)
                    .SetRecipientUserName(emailContentBodyDto.UserProfile.PID)
                    .SetRecipientUserEmail(emailContentBodyDto.UserProfile.EMail)
                    .Build();
        }
    }
}
