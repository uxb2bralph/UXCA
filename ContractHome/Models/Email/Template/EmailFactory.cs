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

        public async void SendEmailToCustomer(string mailTo, IEmailContent emailContent)
        {

            SendEmailToCustomer(emailTo: mailTo,
                            subject: emailContent.Subject,
                            body: await emailContent.GetBody.GetViewRenderString());
        }

        public async void SendEmailToCustomer(IEmailContent emailContent)
        {
            SendEmailToCustomer(emailTo: emailContent.GetBody.UserEmail,
                            subject: emailContent.Subject,
                            body: await emailContent.GetBody.GetViewRenderString());
        }

        public async void SendEmailToCustomer(string emailTo, string subject, string body)
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
            FileLogger.Logger.Error(emailTo);
            await _mailService.SendMailAsync(mailData, default);

        }
        public MailData GetEmailToSystem(string subject, string body)
        {
            List<string> emailList = new List<string>
            {
                "systest@uxb2b.com"
            };

            return new MailData(
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

        }

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

        public IEmailContent GetLoginFailed(string a, string b)
        {
            var emailContent = _emailContents.OfType<LoginFailed>()
                .FirstOrDefault()!;
            emailContent.CreateBody(emailUserName: a, mailTo: b);
            return emailContent;
        }

        public IEmailContent GetPasswordUpdated(string a, string b)
        {
            var emailContent = _emailContents.OfType<PasswordUpdated>()
                .FirstOrDefault()!;

            emailContent.CreateBody(emailUserName: a, mailTo: b);
            return emailContent;
        }

        public IEmailContent GetLoginSuccessed(string a, string b)
        {
            var emailContent = _emailContents.OfType<LoginSuccessed>()
                .FirstOrDefault()!;

            emailContent.CreateBody(emailUserName: a, mailTo: b);
            return emailContent;
        }
    }
}
