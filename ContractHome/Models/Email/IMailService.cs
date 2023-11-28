namespace ContractHome.Models.Email
{
    public interface IMailService
    {
        bool SendMail(MailData mailData);
    }
}
