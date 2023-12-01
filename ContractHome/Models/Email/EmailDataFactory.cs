using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Options;

namespace ContractHome.Models.Email
{
    public class EmailDataFactory
    {
        private readonly MailSettings _mailSettings;
        public EmailDataFactory(IOptions<MailSettings> mailSettingsOptions)
        {
            _mailSettings = mailSettingsOptions.Value;
        }
        public MailData GetMailDataToCustomer(string email, string subject, string body)
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
        public MailData GetMailDataToSystemNotify(string subject, string body)
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
