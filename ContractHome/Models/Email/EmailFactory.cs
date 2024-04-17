using ContractHome.Models.Email.Template;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Options;

namespace ContractHome.Models.Email
{
    public class EmailFactory
    {
        private readonly MailSettings _mailSettings;
        public EmailFactory(IOptions<MailSettings> mailSettingsOptions)
        {
            _mailSettings = mailSettingsOptions.Value;
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

        public async Task<MailData> GetEmailToCustomer(EmailBody emailBody)
        {
            List<string> emailList = new List<string>
            {
                emailBody.UserEmail
            };
            var emailBodyString = await emailBody.GetViewRenderString();

            return new MailData(
                to: emailList,
                subject: GetEmailTitle(emailBody.TemplateItem),
                body: emailBodyString,
                from: _mailSettings.From,
                displayName: _mailSettings.DisplayName,
                replyTo: null,
                replyToName: null,
                bcc: null,
                cc: null
                );

        }

        public MailData GetEmailToCustomer(string email, string subject, string body)
        {
            List<string> emailList = new List<string>
            {
                email
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
