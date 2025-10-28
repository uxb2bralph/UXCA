using CommonLib.Core.Utility;
using MailKit;
using Microsoft.Extensions.Options;

namespace ContractHome.Models.Email.Template
{
    public class EmailFactory
    {
        private readonly MailSettings _mailSettings;
        private readonly IMailService _mailService;
        private readonly IEnumerable<IEmailContent> _emailContents;
        public EmailFactory(IEnumerable<IEmailContent> emailContents,
            IOptions<MailSettings> mailSettingsOptions,
            IMailService mailService)
        {
            _emailContents = emailContents;
            _mailSettings = mailSettingsOptions.Value;
            _mailService = mailService;
        }

        public async Task SendEmailToCustomer(string mailTo, IEmailContent emailContent)
        {

            await SendEmailToCustomer(emailTo: mailTo,
                            subject: emailContent.Subject,
                            body: await emailContent.GetBody.GetViewRenderString());
        }

        public async void SendEmailToCustomer(IEmailContent emailContent)
        {
            await SendEmailToCustomer(emailTo: emailContent.GetBody.UserEmail,
                            subject: emailContent.Subject,
                            body: await emailContent.GetBody.GetViewRenderString());
        }

        public async Task SendEmailToCustomer(string emailTo, string subject, string body)
        {
            List<string> emailList = new List<string>
            {
                emailTo
            };

            MailData mailData = new MailData(
                to: emailList,
                subject: subject,
                body: body,
                from: _mailSettings.From,
                displayName: _mailSettings.DisplayName,
                replyTo: null,
                replyToName: null,
                bcc: null,
                cc: null
            );
            //FileLogger.Logger.Error(emailTo);
            await _mailService.SendMailAsync(mailData, default);

        }
        //public MailData GetEmailToSystem(string subject, string body)
        //{
        //    List<string> emailList = new List<string>
        //    {
        //        "systest@uxb2b.com"
        //    };

        //    return new MailData(
        //        to: emailList,
        //        subject: subject,
        //        body: body,
        //        from: _mailSettings.From,
        //        displayName: _mailSettings.DisplayName,
        //        replyTo: null,
        //        replyToName: null,
        //        bcc: null,
        //        cc: null
        //        );

        //}

        public IEmailContent GetNotifySeal()
        {
            return _emailContents.OfType<NotifySeal>()
                .FirstOrDefault()!;
        }

        public IEmailContent GetNotifySeal(EmailContentBodyDto dto)
        {
            var emailContent = GetNotifySeal();
            emailContent.CreateBody(dto);
            return emailContent;
        }

        public IEmailContent GetNotifySign()
        {
            return _emailContents.OfType<NotifySign>()
                .FirstOrDefault()!;
        }

        public IEmailContent GetNotifySign(EmailContentBodyDto dto)
        {
            var emailContent = GetNotifySign();
            emailContent.CreateBody(dto);
            return emailContent;
        }

        public IEmailContent GetLoginFailed(string emailUserName, string email)
        {
            var emailContent = _emailContents.OfType<LoginFailed>()
                .FirstOrDefault()!;
            emailContent.CreateBody(emailUserName: emailUserName, mailTo: email);
            return emailContent;
        }

        public IEmailContent GetPasswordUpdated(string emailUserName, string email)
        {
            var emailContent = _emailContents.OfType<PasswordUpdated>()
                .FirstOrDefault()!;

            emailContent.CreateBody(emailUserName: emailUserName, mailTo: email);
            return emailContent;
        }

        public IEmailContent GetApplyPassword(EmailContentBodyDto dto)
        {
            var emailContent = _emailContents.OfType<ApplyPassword>()
                .FirstOrDefault()!;
            emailContent.CreateBody(dto);
            return emailContent;
        }

        public IEmailContent GetFinishContract(EmailContentBodyDto dto)
        {
            var emailContent = _emailContents.OfType<FinishContract>()
                .FirstOrDefault()!;
            emailContent.CreateBody(dto);
            return emailContent;
        }

        public IEmailContent GetTerminationContract(EmailContentBodyDto dto)
        {
            var emailContent = _emailContents.OfType<TerminationContract>()
                .FirstOrDefault()!;
            emailContent.CreateBody(dto);
            return emailContent;
        }

        public IEmailContent GetTerminationContract()
        {
            var emailContent = _emailContents.OfType<TerminationContract>()
                .FirstOrDefault()!;
            return emailContent;
        }

        public IEmailContent GetTerminationPrivilege()
        {
            var emailContent = _emailContents.OfType<TerminationPrivilege>()
                .FirstOrDefault()!;
            return emailContent;
        }

        public IEmailContent GetPendingTerminationPrivilege()
        {
            var emailContent = _emailContents.OfType<PendingTerminationPrivilege>()
                .FirstOrDefault()!;
            return emailContent;
        }

        public IEmailContent GetLoginSuccessed(string emailUserName, string email)
        {
            var emailContent = _emailContents.OfType<LoginSuccessed>()
                .FirstOrDefault()!;

            emailContent.CreateBody(emailUserName: emailUserName, mailTo: email);
            return emailContent;
        }
    }
}
