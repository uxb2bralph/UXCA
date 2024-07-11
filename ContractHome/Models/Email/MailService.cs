using CommonLib.Core.Utility;
using ContractHome.Controllers;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net;

namespace ContractHome.Models.Email
{
    public class MailService : IMailService
    {
        private readonly MailSettings _mailSettings;
        public MailService(IOptionsMonitor<MailSettings> mailSettingsOptions)
        {
            _mailSettings = mailSettingsOptions.CurrentValue;
            //_mailSettings = mailSettingsOptions.Value;
        }

        public async Task<bool> SendMailAsync(MailData mailData, CancellationToken ct = default)
        {
            
            if (_mailSettings.Enable==false) { return false; }

            try
            {
                // Initialize a new instance of the MimeKit.MimeMessage class
                var mail = new MimeMessage();

                #region Sender / Receiver
                // Sender
                mail.From.Add(new MailboxAddress(_mailSettings.DisplayName, mailData.From ?? _mailSettings.From));
                mail.Sender = new MailboxAddress(mailData.DisplayName ?? _mailSettings.DisplayName, mailData.From ?? _mailSettings.From);

                // Receiver
                foreach (string mailAddress in mailData.To)
                    mail.To.Add(MailboxAddress.Parse(mailAddress));

                // Set Reply to if specified in mail data
                if (!string.IsNullOrEmpty(mailData.ReplyTo))
                    mail.ReplyTo.Add(new MailboxAddress(mailData.ReplyToName, mailData.ReplyTo));

                // BCC
                // Check if a BCC was supplied in the request
                if (mailData.Bcc != null)
                {
                    // Get only addresses where value is not null or with whitespace. x = value of address
                    foreach (string mailAddress in mailData.Bcc.Where(x => !string.IsNullOrWhiteSpace(x)))
                        mail.Bcc.Add(MailboxAddress.Parse(mailAddress.Trim()));
                }

                // CC
                // Check if a CC address was supplied in the request
                if (mailData.Cc != null)
                {
                    foreach (string mailAddress in mailData.Cc.Where(x => !string.IsNullOrWhiteSpace(x)))
                        mail.Cc.Add(MailboxAddress.Parse(mailAddress.Trim()));
                }
                #endregion

                #region Content

                // Add Content to Mime Message
                var body = new BodyBuilder();
                mail.Subject = mailData.Subject;
                body.HtmlBody = mailData.Body;
                mail.Body = body.ToMessageBody();

                #endregion

                #region Send Mail

                using var smtp = new SmtpClient();

                if (_mailSettings.UseSSL)
                {
                    await smtp.ConnectAsync(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.SslOnConnect, ct);
                    await smtp.AuthenticateAsync(_mailSettings.UserName, _mailSettings.Password, ct);
                }
                /*
                else if (_mailSettings.UseStartTls)
                {
                    await smtp.ConnectAsync(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.StartTls, ct);
                }
                */
                else if (_mailSettings.DefaultNetworkCredentials) 
                {
                    await smtp.ConnectAsync(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.None, ct);
                }

                await smtp.SendAsync(mail, ct);
                //FileLogger.Logger.Error($"Mail To:{mail.To.ToString()}");
                await smtp.DisconnectAsync(true, ct);

                #endregion

                return true;

            }
            catch (Exception ex)
            {
                //https://stackoverflow.com/questions/31637497/how-to-handle-socket-exception
                FileLogger.Logger.Error(ex.ToString());
                return false;
            }
        }

    }
}
