using ContractHome.Models.Email.Template;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Asn1.Pkcs;

namespace ContractHome.Models.Email
{
    public class EmailFactory
    {
        private readonly MailSettings _mailSettings;
        private IEmailBodyBuilder _emailBodyBuilder;
        private readonly IMailService _mailService;
        public EmailFactory(IOptions<MailSettings> mailSettingsOptions,
            IEmailBodyBuilder emailBodyBuilder,
            IMailService mailService)
        {
            _mailSettings = mailSettingsOptions.Value;
            _emailBodyBuilder = emailBodyBuilder;
            _mailService = mailService;
        }

        internal MailSettings MailSettings => _mailSettings;
        public string GetEmailTitle(EmailBody.EmailTemplate emailTemplate)
        {
            if (emailTemplate == EmailBody.EmailTemplate.NotifySeal)
                return @"[安心簽]用印通知信";
            else if (emailTemplate == EmailBody.EmailTemplate.NotifySign)
                return @"[安心簽]簽署通知信";
            else
                return @"[安心簽]通知信";

        }



        public async void SendEmailToCustomer(EmailBody emailBody)
        {
            var emailBodyString = await emailBody.GetViewRenderString();
            SendEmailToCustomer(email: emailBody.UserEmail, subject: GetEmailTitle(emailBody.TemplateItem), body: emailBodyString);
        }

        public async void SendEmailToCustomer(string email, string subject, string body)
        {
            List<string> emailList = new List<string>
            {
                email
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
            //wait to do...沒有await可以嗎?
            _mailService.SendMailAsync(mailData, default);

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
    }
}
