namespace ContractHome.Models.Email
{
    public interface IMailService
    {
        Task<bool> SendMailAsync(MailData mailData, CancellationToken ct = default);
    }
}
