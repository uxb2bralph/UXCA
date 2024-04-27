namespace ContractHome.Models.Email.Template
{
    public interface IEmailContent
    {
        EmailBody GetBody { get; }
        void CreateBody(string emailUserName, string mailTo);
        void CreateBody(EmailContentBodyDto emailContentBodyDto);
        string Subject { get; }
    }
}